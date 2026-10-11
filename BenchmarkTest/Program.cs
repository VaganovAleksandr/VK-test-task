using System.Diagnostics;
using System.Management;
using System.Text.Json.Nodes;

namespace WukongBenchmarkAutomation
{
    class Program
    {
        private static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string ConfigPath = Path.Combine(LocalAppData, @"b1\Saved\Config\Windows\GameUserSettings.ini");
        private static readonly string SaveGamePath = Path.Combine(LocalAppData, @"b1\Saved\SaveGames");
        private static readonly string SteamLaunchUri = "steam://run/3132990";

        private static SystemHardwareInfo GetSystemHardwareInfo()
        {
            var info = new SystemHardwareInfo();

            using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
            {
                foreach (var obj in searcher.Get())
                {
                    info.CpuName = obj["Name"]?.ToString().Trim();
                    break;
                }
            }

            using (var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController"))
            {
                foreach (var obj in searcher.Get())
                {
                    string gpuName = obj["Name"]?.ToString();
                    if (!string.IsNullOrEmpty(gpuName) && !gpuName.Contains("Basic Display"))
                    {
                        info.GpuName = gpuName;
                        break;
                    }
                }
            }

            using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicsMemory FROM Win32_ComputerSystem"))
            {
                foreach (var obj in searcher.Get())
                {
                    if (ulong.TryParse(obj["TotalPhysicalMemory"]?.ToString(), out ulong bytes))
                    {
                        info.RamGb = Math.Round(bytes / (1024.0 * 1024.0 * 1024.0), 1);
                    }
                }
            }
            
            info.OsVersion = Environment.OSVersion.ToString();
            return info;
        }

        private static void PrintHardwareInfo(SystemHardwareInfo info)
        {
            Console.WriteLine($"CPU Name: {info.CpuName}");
            Console.WriteLine($"Gpu Name: {info.GpuName}");
            Console.WriteLine($"Ram Gb: {info.RamGb}");
            Console.WriteLine($"OS Version: {info.OsVersion}");
        }

        private static void EnsureConfigFileExists()
        {
            string dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath, "[/Script/Engine.GameUserSettings]\n[ScalabilityGroups]\n");
            }
        }

        private static void UpdateIniValue(string section, string key, string value)
        {
            if (!File.Exists(ConfigPath)) return;
            
            var lines = File.ReadAllLines(ConfigPath).ToList();
            var sectionIndex =
                lines.FindIndex(line => line.Trim().Equals($"[{section}]", StringComparison.OrdinalIgnoreCase));

            if (sectionIndex == -1)
            {
                lines.Add($"[{section}]");
                lines.Add($"{key}={value}");
            }
            else
            {
                var keyIndex = lines.FindIndex(sectionIndex + 1,
                    line => line.Trim().StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase));
                if (keyIndex != -1 &&
                    (keyIndex < lines.FindIndex(sectionIndex + 1, line => line.Trim().StartsWith("["))))
                {
                    lines[keyIndex] = $"{key}={value}";
                }
                else
                {
                    lines.Insert(sectionIndex + 1, $"{key}={value}");
                }
            }

            File.ReadAllText(ConfigPath);
            File.WriteAllLines(ConfigPath, lines);
        }
        
        private static void ConfigureBenchmark(bool isGpuTest)
        {
            EnsureConfigFileExists();

            if (!isGpuTest)
            {
                UpdateIniValue("ScalabilityGroups", "sg.ResolutionQuality", "50");
                UpdateIniValue("ScalabilityGroups", "sg.ViewDistanceQuality", "0");
                UpdateIniValue("ScalabilityGroups", "sg.AntiAliasingQuality", "0");
                UpdateIniValue("ScalabilityGroups", "sg.ShadowQuality", "0");
                UpdateIniValue("ScalabilityGroups", "sg.GlobalIlluminationQuality", "0");
                UpdateIniValue("ScalabilityGroups", "sg.ReflectionQuality", "0");
                UpdateIniValue("ScalabilityGroups", "sg.PostProcessQuality", "0");
                UpdateIniValue("ScalabilityGroups", "sg.TextureQuality", "0");
                UpdateIniValue("ScalabilityGroups", "sg.EffectsQuality", "0");
                UpdateIniValue("ScalabilityGroups", "sg.FoliageQuality", "0");
                UpdateIniValue("/script/Engine.GameUserSettings", "ResolutionSizeX", "1280");
                UpdateIniValue("/script/Engine.GameUserSettings", "ResolutionSizeY", "720");
                UpdateIniValue("/script/Engine.GameUserSettings", "bUseRayTracing", "False");
            }
            else
            {
                UpdateIniValue("ScalabilityGroups", "sg.ResolutionQuality", "100");
                UpdateIniValue("ScalabilityGroups", "sg.ViewDistanceQuality", "3");
                UpdateIniValue("ScalabilityGroups", "sg.AntiAliasingQuality", "3");
                UpdateIniValue("ScalabilityGroups", "sg.ShadowQuality", "3");
                UpdateIniValue("ScalabilityGroups", "sg.GlobalIlluminationQuality", "3");
                UpdateIniValue("ScalabilityGroups", "sg.ReflectionQuality", "3");
                UpdateIniValue("ScalabilityGroups", "sg.PostProcessQuality", "3");
                UpdateIniValue("ScalabilityGroups", "sg.TextureQuality", "3");
                UpdateIniValue("ScalabilityGroups", "sg.EffectsQuality", "3");
                UpdateIniValue("ScalabilityGroups", "sg.FoliageQuality", "3");
                UpdateIniValue("/script/Engine.GameUserSettings", "ResolutionSizeX", "1920");
                UpdateIniValue("/script/Engine.GameUserSettings", "ResolutionSizeY", "1080");
                UpdateIniValue("/script/Engine.GameUserSettings", "bUseRayTracing", "True");
            }
        }

        private static void RunBenchmarkProcess()
        {
            Console.WriteLine("Starting benchmark");

            Process.Start(new ProcessStartInfo { FileName = SteamLaunchUri, UseShellExecute = true });
            
            Thread.Sleep(5000);
            
            Process benchmarkProcess = null;
            while (benchmarkProcess == null)
            {
                benchmarkProcess = Process.GetProcessesByName("b1-Win64-Shipping").FirstOrDefault() ?? Process.GetProcessesByName("b1").FirstOrDefault();
                
                Thread.Sleep(1000);
            }
            
            Console.WriteLine("Benchmark has started. Waiting it to finish");
            benchmarkProcess.WaitForExit();
            Console.WriteLine("Benchmark finished");
            Thread.Sleep(3000);
        }

        private static BenchmarkResult GetLatestBenchmarkResult(DateTime testStartTime)
        {
            var result = new BenchmarkResult();

            if (!Directory.Exists(SaveGamePath))
            {
                result.SettingsSummary = "Directory with saved results not found";
                return result;
            }
            
            var latestFile = new DirectoryInfo(SaveGamePath).GetFiles("*.json", SearchOption.AllDirectories)
                    .Where(file => file.LastWriteTime >= testStartTime.AddSeconds(-10))
                    .OrderByDescending(file => file.LastWriteTime).FirstOrDefault();
            if (latestFile == null)
            {
                latestFile = new DirectoryInfo(SaveGamePath).GetFiles(".", SearchOption.AllDirectories)
                    .OrderByDescending(file => file.LastWriteTime).FirstOrDefault();
            }

            if (latestFile != null)
            {
                try
                {
                    var jsonContent = File.ReadAllText(latestFile.FullName);
                    var node = JsonNode.Parse(jsonContent);

                    if (node != null)
                    {
                        result.AvgFps = node["AvgFPS"]?.GetValue<double>() ?? node["fps"]?.GetValue<double>() ?? 0;
                        result.MinFps = node["MinFPS"]?.GetValue<double>() ?? 0;
                        result.MaxFps = node["MaxFPS"]?.GetValue<double>() ?? 0;

                        var resX = node["ResolutionX"]?.ToString() ?? "N/A";
                        var resY = node["ResolutionY"]?.ToString() ?? "N/A";
                        var preset = node["GraphicsPreset"]?.ToString() ?? "Custom";

                        result.SettingsSummary = $"Resolution: {resX}x{resY}, Preset: {preset}";
                    }
                }
                catch
                {
                    result.SettingsSummary = "Error parsing results file";
                }
            }
            else
            {
                result.SettingsSummary = "File with saved results not found";
            }

            return result;
        }
        
        static void Main(string[] args)
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("Black Myth: Wukong Benchmark Automation");
            Console.WriteLine("=========================================");
            
            Console.WriteLine("Collecting System Characteristics");
            var sysInfo = GetSystemHardwareInfo();
            PrintHardwareInfo(sysInfo);

            Console.WriteLine("CPU test");
            ConfigureBenchmark(false);
            DateTime cpuStartTime = DateTime.Now;
            RunBenchmarkProcess();
            BenchmarkResult result_cpu = GetLatestBenchmarkResult(cpuStartTime);
            
            Console.WriteLine("GPU test");
            ConfigureBenchmark(true);
            DateTime gpuStartTime = DateTime.Now;
            RunBenchmarkProcess();
            BenchmarkResult result_gpu = GetLatestBenchmarkResult(gpuStartTime);

            Console.Clear();
            Console.WriteLine("=======================");
            Console.WriteLine("Final benchmark results");
            Console.WriteLine("=======================");
            
            PrintHardwareInfo(sysInfo);
            Console.WriteLine("---CPU test results---");
            Console.WriteLine($"Settings: {result_cpu.SettingsSummary}");
            Console.WriteLine($"AvgFPS: {result_cpu.AvgFps}");
            Console.WriteLine($"MinFPS: {result_cpu.MinFps}");
            Console.WriteLine($"MaxFPS: {result_cpu.MaxFps}");
            
            Console.WriteLine("---GPU test results---");
            Console.WriteLine($"Settings: {result_gpu.SettingsSummary}");
            Console.WriteLine($"AvgFPS: {result_gpu.AvgFps}");
            Console.WriteLine($"MinFPS: {result_gpu.MinFps}");
            Console.WriteLine($"MaxFPS: {result_gpu.MaxFps}");

            Console.WriteLine("Benchmark finished");
        }
    }

    public class SystemHardwareInfo
    {
        public string CpuName { get; set; } = "Not defined";
        public string GpuName { get; set; } = "Not defined";
        public double RamGb { get; set; }
        public string OsVersion { get; set; } = "Not defined";
    }

    public class BenchmarkResult
    {
        public double AvgFps { get; set; }
        public double MinFps { get; set; }
        public double MaxFps { get; set; }
        public string SettingsSummary { get; set; } = "Not defined";
    }
}