package com.freebike.ble;

import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothGatt;
import android.bluetooth.BluetoothGattCallback;
import android.bluetooth.BluetoothGattCharacteristic;
import android.bluetooth.BluetoothGattDescriptor;
import android.bluetooth.BluetoothGattService;
import android.bluetooth.BluetoothManager;
import android.bluetooth.BluetoothProfile;
import android.bluetooth.le.BluetoothLeScanner;
import android.bluetooth.le.ScanCallback;
import android.bluetooth.le.ScanResult;
import android.bluetooth.le.ScanSettings;
import android.content.Context;
import android.os.Handler;
import android.os.ParcelUuid;
import android.os.Looper;
import android.util.Log;

import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.UUID;

/**
 * ThinkRider A1 BLE 桥接（标准 FTMS / CPS，不依赖厂商 App）
 * 通过反射调用 UnityPlayer，避免编译期依赖 Unity 的 classes.jar。
 *
 * 消息格式（UnitySendMessage -> GameObject "callbackObject" -> OnBleMessage）：
 *   SCAN|name|address|rssi
 *   STATE|CONNECTED
 *   STATE|DISCONNECTED|status
 *   SVC|uuid
 *   NOTIFY|uuid|hex
 *   READ|uuid|hex
 *   ERR|message
 */
public class ThinkRiderBlePlugin {

    private static final String TAG = "ThinkRiderBle";

    private static final UUID UUID_FTMS = uuid16("1826");
    private static final UUID UUID_FTMS_INDOOR_DATA = uuid16("2ad2");
    private static final UUID UUID_FTMS_FEATURE = uuid16("2acc");
    private static final UUID UUID_CPS = uuid16("1818");
    private static final UUID UUID_CPS_MEASUREMENT = uuid16("2a63");
    private static final UUID UUID_CCCD = uuid16("2902");

    private static Context context;
    private static Handler mainHandler;
    private static String callbackObject = "ThinkRiderBleCallback";

    private static BluetoothGatt gatt;
    private static BluetoothLeScanner scanner;
    private static boolean scanning = false;
    private static String namePrefix = "ThinkRider";
    private static final Set<String> seenDevices = new HashSet<>();
    private static final Set<String> dbgSeen = new HashSet<>();
    private static int advLogged;
    private static final List<BluetoothGattDescriptor> descriptorQueue = new ArrayList<>();

    private static UUID uuid16(String shortUuid) {
        return UUID.fromString("0000" + shortUuid + "-0000-1000-8000-00805f9b34fb");
    }

    private static Context getContext() {
        if (context != null) return context;
        try {
            Class<?> player = Class.forName("com.unity3d.player.UnityPlayer");
            Field f = player.getField("currentActivity");
            context = (Context) f.get(null);
        } catch (Throwable t) {
            Log.e(TAG, "getContext failed", t);
        }
        return context;
    }

    private static void send(final String message) {
        if (mainHandler == null) mainHandler = new Handler(Looper.getMainLooper());
        mainHandler.post(new Runnable() {
            @Override
            public void run() {
                try {
                    Class<?> player = Class.forName("com.unity3d.player.UnityPlayer");
                    Method m = player.getMethod("UnitySendMessage", String.class, String.class, String.class);
                    m.invoke(null, callbackObject, "OnBleMessage", message);
                } catch (Throwable t) {
                    Log.e(TAG, "send failed: " + message, t);
                }
            }
        });
    }

    private static void sendError(String msg) {
        send("ERR|" + msg);
    }

    public static void init(String callbackName) {
        if (callbackName != null && callbackName.length() > 0) callbackObject = callbackName;
        mainHandler = new Handler(Looper.getMainLooper());
        getContext();
        Log.i(TAG, "init, callback=" + callbackObject);
    }

    private static BluetoothLeScanner getScanner() {
        if (scanner != null) return scanner;
        Context ctx = getContext();
        if (ctx == null) return null;
        BluetoothManager manager = (BluetoothManager) ctx.getSystemService(Context.BLUETOOTH_SERVICE);
        if (manager == null) {
            sendError("no BluetoothManager");
            return null;
        }
        BluetoothAdapter adapter = manager.getAdapter();
        if (adapter == null || !adapter.isEnabled()) {
            sendError("bluetooth disabled");
            return null;
        }
        scanner = adapter.getBluetoothLeScanner();
        return scanner;
    }

    public static void startScan(String prefix) {
        if (prefix != null && prefix.length() > 0) namePrefix = prefix;
        BluetoothLeScanner s = getScanner();
        if (s == null) {
            sendError("scanner unavailable");
            return;
        }
        if (scanning) return;
        seenDevices.clear();
        dbgSeen.clear();
        advLogged = 0;
        scanning = true;
        try {
            ScanSettings settings = new ScanSettings.Builder()
                    .setScanMode(ScanSettings.SCAN_MODE_LOW_LATENCY)
                    .setCallbackType(ScanSettings.CALLBACK_TYPE_ALL_MATCHES)
                    .setReportDelay(0)
                    .build();
            s.startScan(null, settings, scanCallback);
            Log.i(TAG, "scan start, prefix=" + namePrefix);
        } catch (Throwable t) {
            scanning = false;
            sendError("startScan: " + t.getMessage());
        }
    }

    public static void stopScan() {
        if (scanner == null || !scanning) return;
        try {
            scanner.stopScan(scanCallback);
        } catch (Throwable ignored) {
        }
        scanning = false;
    }

    private static final ScanCallback scanCallback = new ScanCallback() {
        @Override
        public void onScanResult(int callbackType, ScanResult result) {
            try {
                BluetoothDevice device = result.getDevice();
                String address = device.getAddress();
                android.bluetooth.le.ScanRecord rec = result.getScanRecord();

                String uuids = "";
                boolean hasCps = false;
                if (rec != null && rec.getServiceUuids() != null) {
                    for (ParcelUuid pu : rec.getServiceUuids()) {
                        String u = pu.getUuid().toString();
                        uuids += u.substring(4, 8) + " ";
                        if (u.startsWith("00001818")) hasCps = true;
                    }
                }

                // Android 上 device.getName() 经常返回 null（名字只在扫描响应里）；
                // 拿不到名字时仍然可以靠广播里的 0x1818 服务识别
                String name = rec != null ? rec.getDeviceName() : null;
                if (name == null || name.length() == 0) {
                    try { name = device.getName(); } catch (Throwable ignored) { }
                }
                if (advLogged < 80) { advLogged++; Log.i(TAG, "adv name=" + name + " addr=" + address + " rssi=" + result.getRssi() + " uuids=" + uuids); }

                boolean nameMatch = name != null && namePrefix != null &&
                        name.toLowerCase(java.util.Locale.ROOT).startsWith(namePrefix.toLowerCase(java.util.Locale.ROOT));
                if (!hasCps && !nameMatch) {
                    if (dbgSeen.add("s:" + address)) send("DBG|skip|" + name + "|" + result.getRssi() + "|" + uuids);
                    return;
                }
                if (seenDevices.contains(address)) return;
                seenDevices.add(address);
                send("SCAN|" + (name == null ? "A1" : name) + "|" + address + "|" + result.getRssi());
            } catch (Throwable t) {
                Log.w(TAG, "onScanResult", t);
            }
        }

        @Override
        public void onScanFailed(int errorCode) {
            scanning = false;
            sendError("scan failed: " + errorCode);
        }
    };

    public static void connect(String address) {
        stopScan();
        Context ctx = getContext();
        if (ctx == null) {
            sendError("no context");
            return;
        }
        try {
            BluetoothManager manager = (BluetoothManager) ctx.getSystemService(Context.BLUETOOTH_SERVICE);
            BluetoothAdapter adapter = manager.getAdapter();
            BluetoothDevice device = adapter.getRemoteDevice(address);
            if (gatt != null) {
                gatt.close();
                gatt = null;
            }
            gatt = device.connectGatt(ctx, false, gattCallback, BluetoothDevice.TRANSPORT_LE);
            send("STATE|CONNECTING");
            Log.i(TAG, "connect " + address);
        } catch (Throwable t) {
            sendError("connect: " + t.getMessage());
        }
    }

    public static void disconnect() {
        stopScan();
        try {
            if (gatt != null) {
                gatt.disconnect();
                gatt.close();
                gatt = null;
            }
        } catch (Throwable ignored) {
        }
        send("STATE|DISCONNECTED|manual");
    }

    public static boolean isConnected() {
        return gatt != null;
    }

    private static final BluetoothGattCallback gattCallback = new BluetoothGattCallback() {
        @Override
        public void onConnectionStateChange(BluetoothGatt g, int status, int newState) {
            if (newState == BluetoothProfile.STATE_CONNECTED) {
                send("STATE|CONNECTED");
                // 不要在这里 requestMtu：Android 上 MTU 请求和 discoverServices 并发会互相打断，
                // 表现为服务发现失败/收不到通知。CPS 帧只有 10 字节，默认 MTU 足够。
                try {
                    g.discoverServices();
                } catch (Throwable t) {
                    sendError("discoverServices: " + t.getMessage());
                }
            } else if (newState == BluetoothProfile.STATE_DISCONNECTED) {
                send("STATE|DISCONNECTED|" + status);
                try {
                    g.close();
                } catch (Throwable ignored) {
                }
                if (gatt == g) gatt = null;
            }
        }

        @Override
        public void onServicesDiscovered(BluetoothGatt g, int status) {
            if (status != BluetoothGatt.GATT_SUCCESS) {
                sendError("service discovery status=" + status);
                return;
            }
            descriptorQueue.clear();
            for (BluetoothGattService service : g.getServices()) {
                send("SVC|" + service.getUuid().toString());
            }
            for (BluetoothGattService service : g.getServices()) {
                for (BluetoothGattCharacteristic ch : service.getCharacteristics()) {
                    UUID u = ch.getUuid();
                    if (u.equals(UUID_FTMS_INDOOR_DATA) || u.equals(UUID_CPS_MEASUREMENT)) {
                        enableNotify(g, ch);
                    } else if (u.equals(UUID_FTMS_FEATURE)) {
                        try {
                            g.readCharacteristic(ch);
                        } catch (Throwable ignored) {
                        }
                    }
                }
            }
            writeNextDescriptor(g);
        }

        @Override
        public void onCharacteristicChanged(BluetoothGatt g, BluetoothGattCharacteristic ch) {
            notifyValue(ch.getUuid(), ch.getValue());
        }

        @Override
        public void onCharacteristicRead(BluetoothGatt g, BluetoothGattCharacteristic ch, int status) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                send("READ|" + ch.getUuid().toString() + "|" + toHex(ch.getValue()));
            }
        }

        @Override
        public void onDescriptorWrite(BluetoothGatt g, BluetoothGattDescriptor descriptor, int status) {
            if (status != BluetoothGatt.GATT_SUCCESS)
                sendError("cccd write status=" + status);
            writeNextDescriptor(g);
        }
    };

    private static void enableNotify(BluetoothGatt g, BluetoothGattCharacteristic ch) {
        try {
            g.setCharacteristicNotification(ch, true);
            BluetoothGattDescriptor cccd = ch.getDescriptor(UUID_CCCD);
            if (cccd != null) {
                cccd.setValue(BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE);
                descriptorQueue.add(cccd);
            }
        } catch (Throwable t) {
            Log.w(TAG, "enableNotify", t);
        }
    }

    private static void writeNextDescriptor(BluetoothGatt g) {
        if (descriptorQueue.isEmpty()) return;
        BluetoothGattDescriptor d = descriptorQueue.remove(0);
        try {
            g.writeDescriptor(d);
        } catch (Throwable t) {
            Log.w(TAG, "writeDescriptor", t);
            writeNextDescriptor(g);
        }
    }

    private static void notifyValue(UUID uuid, byte[] value) {
        if (value == null) return;
        send("NOTIFY|" + uuid.toString() + "|" + toHex(value));
    }

    private static String toHex(byte[] data) {
        if (data == null) return "";
        StringBuilder sb = new StringBuilder(data.length * 2);
        for (byte b : data) {
            sb.append(Character.forDigit((b >> 4) & 0xF, 16));
            sb.append(Character.forDigit(b & 0xF, 16));
        }
        return sb.toString();
    }
}
