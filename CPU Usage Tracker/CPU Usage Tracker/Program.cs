using System;
using System.Diagnostics;
using System.Threading;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace VideoPlaybackTest
{
    class Program
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        static double maxCpuSpeed = 0;
        static public double getMaxCPUSpeed()
        {
            ManagementObject Mo = new ManagementObject("Win32_Processor.DeviceID='CPU0'");
            uint sp = (uint)(Mo["MaxClockSpeed"]);
            Mo.Dispose();
            return (double)sp;
        }

        static double getMinValue(double a, double b)
        {
            return a < b ? a : b;
        }
        static double getMaxValue(double a, double b)
        {
            return a > b ? a : b;
        }
        static void Main(string[] args)
        {
            long memKb;
            GetPhysicallyInstalledSystemMemory(out memKb);
            double memGb= ((double)memKb / 1024 / 1024);
            Console.WriteLine(memGb + " GB of RAM installed.");

            while (true)
            {
                Console.Write("Total time (mins):  ");
                string totalTimeStr = Console.ReadLine();
                double totalTime = double.Parse(totalTimeStr);
                Console.WriteLine();

                Console.Write("Record interval (secs):  ");
                string recordIntervalStr = Console.ReadLine();
                double recordInterval = double.Parse(recordIntervalStr);
                Console.WriteLine();

                maxCpuSpeed = getMaxCPUSpeed();

                PerformanceCounter cpuPerformance = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
                PerformanceCounter memoryAvailable = new PerformanceCounter("Memory", "Available MBytes");
                PerformanceCounterCategory pcg = new PerformanceCounterCategory("Network Interface");

                NetworkInterface[] interfaces= NetworkInterface.GetAllNetworkInterfaces();
                long initialBytesSent = interfaces[0].GetIPv4Statistics().BytesSent;
                long tempBytesSent = initialBytesSent;
                long initialBytesRec = interfaces[0].GetIPv4Statistics().BytesReceived;
                long tempBytesRec = initialBytesRec;

                double minCpuFrequency = 0;
                double maxCpuFrequency = 0;
                double totalCpuFrequency = 0;

                double minRamMemory = 0;
                double maxRamMemory = 0;
                double totalRamMemory = 0;

                double minBytesSent = 0;
                double maxBytesSent = 0;
                double minBytesRec = 0;
                double maxBytesRec = 0;

                int numberOfTick = 0;
                var startTimer = DateTime.Now;
                var prevTimer = DateTime.Now;
                while ((totalTime - DateTime.Now.Subtract(startTimer).TotalMinutes)> 0)
                {
                    Thread.Sleep((int)(recordInterval * 1000.0));
                    double cpuFrequency = maxCpuSpeed * cpuPerformance.NextValue() / 100000;
                    totalCpuFrequency += cpuFrequency;

                    double ramMemory = memGb-memoryAvailable.NextValue()/1000;
                    totalRamMemory += ramMemory;

                    double tempSentBytesRate = (interfaces[0].GetIPv4Statistics().BytesSent - tempBytesSent) * 8 / (recordInterval*1000);
                    tempBytesSent = interfaces[0].GetIPv4Statistics().BytesSent;

                    double tempRecBytesRate = (interfaces[0].GetIPv4Statistics().BytesReceived - tempBytesRec) * 8 / (recordInterval * 1000);
                    tempBytesRec= interfaces[0].GetIPv4Statistics().BytesReceived;

                    if (numberOfTick == 0)
                    {
                        minBytesSent = tempSentBytesRate;
                        maxBytesSent = tempSentBytesRate;
                        minBytesRec = tempRecBytesRate;
                        maxBytesRec = tempRecBytesRate;

                    }
                    else if (numberOfTick == 1)
                    {
                        minCpuFrequency = cpuFrequency;
                        maxCpuFrequency = cpuFrequency;

                        minRamMemory = ramMemory;
                        maxRamMemory = ramMemory;
                    }
                    else
                    {
                        minCpuFrequency = getMinValue(minCpuFrequency, cpuFrequency);
                        maxCpuFrequency = getMaxValue(maxCpuFrequency, cpuFrequency);
                        minRamMemory = getMinValue(minRamMemory, ramMemory);
                        maxRamMemory = getMaxValue(maxRamMemory, ramMemory);
                        minBytesSent = getMinValue(minBytesSent, tempSentBytesRate);
                        maxBytesSent = getMaxValue(maxBytesSent, tempSentBytesRate);
                        minBytesRec = getMinValue(minBytesRec, tempRecBytesRate);
                        maxBytesRec = getMaxValue(maxBytesRec, tempRecBytesRate);
                    }
                    if (DateTime.Now.Subtract(prevTimer).TotalSeconds>1)
                    {
                        prevTimer = DateTime.Now;
                        Console.WriteLine();
                        Console.WriteLine("Time passed:  " + DateTime.Now.Subtract(startTimer).Hours.ToString()+ " hr " + DateTime.Now.Subtract(startTimer).Minutes.ToString() + " min " + DateTime.Now.Subtract(startTimer).Seconds.ToString() + " sec ");
                        Console.WriteLine("Cpu Frequency (Ghz):  " + cpuFrequency);
                        Console.WriteLine("Ram Memory (GB):      " + ramMemory);
                        Console.WriteLine("Send (Kbps):          " + tempSentBytesRate);
                        Console.WriteLine("Recieved (Kbps):      " + tempRecBytesRate);
                    }
                    numberOfTick++;
                }
                long totalBytesSent = interfaces[0].GetIPv4Statistics().BytesSent- initialBytesSent;
                long totallBytesRec = interfaces[0].GetIPv4Statistics().BytesReceived- initialBytesRec;
                Console.WriteLine();
                Console.WriteLine("Min Cpu Frequency (Ghz):      " + minCpuFrequency);
                Console.WriteLine("Max Cpu Frequency (Ghz):      " + maxCpuFrequency);
                Console.WriteLine("Avg Cpu Frequency (Ghz):      " + totalCpuFrequency/ (numberOfTick-1));
                Console.WriteLine("Min Ram Memory (GB):          " + minRamMemory);
                Console.WriteLine("Max Ram Memory (GB):          " + maxRamMemory);
                Console.WriteLine("Avg Ram Memory (GB):          " + totalRamMemory / numberOfTick);
                Console.WriteLine("Send minimum (Kbps):          " + minBytesSent);
                Console.WriteLine("Send maximum (Kbps):          " + maxBytesSent);
                Console.WriteLine("Send average (Kbps):          " + totalBytesSent * 8 / (totalTime * 60000.0));
                Console.WriteLine("Recieved minimum (Kbps):      " + minBytesRec);
                Console.WriteLine("Recieved maximum (Kbps):      " + maxBytesRec);
                Console.WriteLine("Recieved average (Kbps):      " + totallBytesRec*8/ (totalTime * 60000.0));
                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }
}
