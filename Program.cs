using System;
using System.Collections.Generic;

namespace CampusSafetyHub
{
    public interface ISensor
    {
        double ReadValue();
        string GetUnit();
    }

    public sealed record AlertEvent(
        string SensorName,
        double Value,
        string Unit,
        string Severity,
        DateTime OccurredAt);

    public sealed class LegacySmokeDetector
    {
        private int smokeLevel;

        public LegacySmokeDetector(int smokeLevel)
        {
            this.smokeLevel = Math.Clamp(smokeLevel, 0, 1000);
        }

        public int GetSmokeLevel() => smokeLevel;
        public string GetDescription() => "Legacy smoke detector";
        public void SetSmokeLevel(int value)
        {
            smokeLevel = Math.Clamp(value, 0, 1000);
        }
    }

    public sealed class SmokeSensorAdapter : ISensor
    {
        private readonly LegacySmokeDetector legacy;

        public SmokeSensorAdapter(LegacySmokeDetector legacy)
        {
            this.legacy = legacy ?? throw new ArgumentNullException(nameof(legacy));
        }

        public double ReadValue()
        {
            return legacy.GetSmokeLevel() / 10.0;
        }

        public string GetUnit() => "% smoke";
    }

    public interface IAlertObserver
    {
        void Update(AlertEvent alertEvent);
    }

    public sealed class SensorMonitor
    {
        private readonly List<IAlertObserver> observers = new();

        public void Attach(IAlertObserver observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
        }

        public void Detach(IAlertObserver observer)
        {
            observers.Remove(observer);
        }

        public void CheckSensor(string sensorName, ISensor sensor)
        {
            ArgumentNullException.ThrowIfNull(sensor);
            double value = sensor.ReadValue();
            string? severity = value switch
            {
                >= 70 => "CRITICAL",
                >= 50 => "WARNING",
                _ => null
            };

            if (severity is null)
            {
                Console.WriteLine($"[NORMAL] {sensorName}: {value:F1}{sensor.GetUnit()} - An toàn.");
                return;
            }

            var alertEvent = new AlertEvent(
                sensorName,
                value,
                sensor.GetUnit(),
                severity,
                DateTime.Now);

            Notify(alertEvent);
        }

        private void Notify(AlertEvent alertEvent)
        {
            foreach (IAlertObserver observer in observers.ToArray())
            {
                try
                {
                    observer.Update(alertEvent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Lỗi tại {observer.GetType().Name}: {ex.Message}");
                }
            }
        }
    }

    public sealed class DashboardObserver : IAlertObserver
    {
        public void Update(AlertEvent alertEvent)
        {
            Console.WriteLine($"[DASHBOARD] {alertEvent.SensorName}: {alertEvent.Value:F1}{alertEvent.Unit} [{alertEvent.Severity}]");
        }
    }

    public sealed class EmailAlertObserver : IAlertObserver
    {
        private readonly string recipientEmail;

        public EmailAlertObserver(string recipientEmail)
        {
            this.recipientEmail = recipientEmail;
        }

        public void Update(AlertEvent alertEvent)
        {
            Console.WriteLine($"[EMAIL -> {recipientEmail}] Cảnh báo {alertEvent.Severity} tại {alertEvent.SensorName} ({alertEvent.Value:F1}{alertEvent.Unit}) lúc {alertEvent.OccurredAt:HH:mm:ss}");
        }
    }

    public sealed class AuditLogObserver : IAlertObserver
    {
        private readonly List<string> logs = new();
        public IReadOnlyList<string> Logs => logs.AsReadOnly();

        public void Update(AlertEvent alertEvent)
        {
            string entry = $"{alertEvent.OccurredAt:yyyy-MM-dd HH:mm:ss} | {alertEvent.Severity} | {alertEvent.SensorName} | {alertEvent.Value:F1}{alertEvent.Unit}";
            logs.Add(entry);
            Console.WriteLine($"[AUDIT LOG] Đã lưu bản ghi: {entry}");
        }
    }

    public interface IEmergencyMediator
    {
        void Notify(object sender, string eventCode);
    }

    public sealed class EmergencyBridgeObserver : IAlertObserver
    {
        private readonly IEmergencyMediator mediator;

        public EmergencyBridgeObserver(IEmergencyMediator mediator)
        {
            this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public void Update(AlertEvent alertEvent)
        {
            if (alertEvent.Severity == "CRITICAL")
            {
                Console.WriteLine($"[BRIDGE] Phát hiện mức CRITICAL từ {alertEvent.SensorName}, kích hoạt điều phối khẩn cấp...");
                mediator.Notify(this, "FIRE_DETECTED");
            }
        }
    }

    public sealed class Alarm
    {
        public bool IsActive { get; private set; }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            Console.WriteLine("  -> [ALARM] Còi báo cháy và đèn chớp khẩn cấp ĐÃ BẬT.");
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            Console.WriteLine("  -> [ALARM] Còi báo cháy ĐÃ TẮT.");
        }
    }

    public sealed class DoorController
    {
        public bool AreDoorsUnlocked { get; private set; }

        public void UnlockAllDoors()
        {
            if (AreDoorsUnlocked) return;
            AreDoorsUnlocked = true;
            Console.WriteLine("  -> [DOOR] Toàn bộ cửa từ phòng máy ĐÃ MỞ KHÓA thoát hiểm.");
        }

        public void LockAllDoors()
        {
            if (!AreDoorsUnlocked) return;
            AreDoorsUnlocked = false;
            Console.WriteLine("  -> [DOOR] Toàn bộ cửa phòng máy ĐÃ KHÓA lại.");
        }
    }

    public sealed class VentilationController
    {
        public bool IsExhaustActive { get; private set; }

        public void ActivateExhaust()
        {
            if (IsExhaustActive) return;
            IsExhaustActive = true;
            Console.WriteLine("  -> [VENTILATION] Hệ thống quạt hút khói công suất cao ĐÃ BẬT.");
        }

        public void StopExhaust()
        {
            if (!IsExhaustActive) return;
            IsExhaustActive = false;
            Console.WriteLine("  -> [VENTILATION] Hệ thống quạt hút khói ĐÃ DỪNG.");
        }
    }

    public sealed class EmergencyMediator : IEmergencyMediator
    {
        private readonly Alarm alarm;
        private readonly DoorController doorController;
        private readonly VentilationController ventilationController;

        public EmergencyMediator(
            Alarm alarm,
            DoorController doorController,
            VentilationController ventilationController)
        {
            this.alarm = alarm;
            this.doorController = doorController;
            this.ventilationController = ventilationController;
        }

        public void Notify(object sender, string eventCode)
        {
            if (eventCode == "FIRE_DETECTED")
            {
                Console.WriteLine("[MEDIATOR] Nhận tín hiệu FIRE_DETECTED. Đang điều phối các thiết bị:");
                alarm.Activate();
                doorController.UnlockAllDoors();
                ventilationController.ActivateExhaust();
            }
            else if (eventCode == "ALL_CLEAR")
            {
                Console.WriteLine("[MEDIATOR] Nhận tín hiệu ALL_CLEAR. Đang khôi phục trạng thái bình thường:");
                alarm.Deactivate();
                doorController.LockAllDoors();
                ventilationController.StopExhaust();
            }
        }
    }

    public sealed class FaultyObserver : IAlertObserver
    {
        public void Update(AlertEvent alertEvent)
        {
            throw new InvalidOperationException("Mất kết nối tới máy chủ phụ.");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var legacyDetector = new LegacySmokeDetector(320);
            ISensor smokeSensor = new SmokeSensorAdapter(legacyDetector);

            var alarm = new Alarm();
            var doors = new DoorController();
            var ventilation = new VentilationController();
            var mediator = new EmergencyMediator(alarm, doors, ventilation);

            var dashboard = new DashboardObserver();
            var emailAlert = new EmailAlertObserver("admin-safety@university.edu.vn");
            var auditLog = new AuditLogObserver();
            var emergencyBridge = new EmergencyBridgeObserver(mediator);
            var faultyObserver = new FaultyObserver();

            var monitor = new SensorMonitor();
            monitor.Attach(dashboard);
            monitor.Attach(dashboard);
            monitor.Attach(emailAlert);
            monitor.Attach(faultyObserver);
            monitor.Attach(auditLog);
            monitor.Attach(emergencyBridge);

            Console.WriteLine("=== KỊCH BẢN 1: MỨC KHÓI BÌNH THƯỜNG (32.0%) ===");
            monitor.CheckSensor("ServerRoom-Smoke-01", smokeSensor);

            Console.WriteLine();
            Console.WriteLine("=== KỊCH BẢN 2: MỨC KHÓI CẢNH BÁO WARNING (62.5%) ===");
            legacyDetector.SetSmokeLevel(625);
            monitor.CheckSensor("ServerRoom-Smoke-01", smokeSensor);

            Console.WriteLine();
            Console.WriteLine("=== KỊCH BẢN 3: MỨC KHÓI NGUY HIỂM CRITICAL (73.5%) ===");
            legacyDetector.SetSmokeLevel(735);
            monitor.CheckSensor("ServerRoom-Smoke-01", smokeSensor);

            Console.WriteLine();
            Console.WriteLine("=== KỊCH BẢN 4: HỦY ĐĂNG KÝ OBSERVER & KHÔI PHỤC HỆ THỐNG ===");
            monitor.Detach(emailAlert);
            monitor.Detach(faultyObserver);
            mediator.Notify("Admin", "ALL_CLEAR");
            legacyDetector.SetSmokeLevel(810);
            monitor.CheckSensor("ServerRoom-Smoke-01", smokeSensor);

            Console.WriteLine();
            Console.WriteLine($"Tổng số bản ghi trong AuditLog: {auditLog.Logs.Count}");
        }
    }
}