using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Drawing;
using System.Threading.Tasks;

namespace Shivamkumar_csharpAssignment
{
    class TimeEntry
    {
        public string? EmployeeName { get; set; }
        public DateTime StarTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
    }

    class EmployeeSummary
    {
        public string Name { get; set; } = "";
        public double TotalHours { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            string url = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";

            Console.WriteLine("Fetching data");
            List<TimeEntry>? entries = await FetchDataAsync(url);

            var employeeHours = entries
                .Where(e => e.EmployeeName != null)
                .GroupBy(e => e.EmployeeName)
                .Select(g => new EmployeeSummary
                {
                    Name = g.Key ?? "Unknown",
                    TotalHours = g.Sum(e => (e.EndTimeUtc - e.StarTimeUtc).TotalHours)
                })
                .OrderByDescending(e => e.TotalHours)
                .ToList();

            GenerateHTML(employeeHours);

            GeneratePieChart(employeeHours);

        }

       
        static async Task<List<TimeEntry>?> FetchDataAsync(string url)
        {
            using HttpClient client = new HttpClient();
            var response = await client.GetStringAsync(url);
            return JsonConvert.DeserializeObject<List<TimeEntry>>(response);
        }

        // HTML Table
        static void GenerateHTML(List<EmployeeSummary> employees)
        {
            string filePath = "EmployeeReport.html";
            using StreamWriter sw = new StreamWriter(filePath);
            sw.WriteLine("<html><head><title>Employee Work Hours</title>");
            sw.WriteLine("<style>");
            sw.WriteLine("table { border-collapse: collapse; width: 60%; margin: 20px auto; font-family: Arial; }");
            sw.WriteLine("th, td { border: 1px solid #333; padding: 10px; text-align: center; }");
            sw.WriteLine("th { background-color: #4CAF50; color: white; }");
            sw.WriteLine(".lowhours { background-color: #ffcccc; }");
            sw.WriteLine("</style></head><body>");
            sw.WriteLine("<h2 style='text-align:center;'>Employee Total Work Hours</h2>");
            sw.WriteLine("<table>");
            sw.WriteLine("<tr><th>Name</th><th>Total Hours Worked</th></tr>");

            foreach (var emp in employees)
            {
                string cssClass = emp.TotalHours < 100 ? " class='lowhours'" : "";
                sw.WriteLine($"<tr{cssClass}><td>{emp.Name}</td><td>{emp.TotalHours:F2}</td></tr>");
            }

            sw.WriteLine("</table></body></html>");
            Console.WriteLine($" HTML file created: {Path.GetFullPath(filePath)}");
        }

        // Pie Chart
        static void GeneratePieChart(List<EmployeeSummary> employees)
        {
            string filePath = "EmployeePieChart.png";
            int width = 800, height = 600;


            using Bitmap bmp = new Bitmap(width, height);
            using Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.White);

            double total = employees.Sum(e => e.TotalHours);
            double avge = employees.Average(e => e.TotalHours);
            float startAngle = 0f;
            Random rand = new Random();

            foreach (var emp in employees)
            {
                float sweepAngle = (float)(emp.TotalHours / total * 360);
                Color color = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
                using Brush brush = new SolidBrush(color);
                g.FillPie(brush, 100, 100, 400, 400, startAngle, sweepAngle);
                g.FillRectangle(brush, 520, (int)startAngle / 2 + 50, 20, 20);
                g.DrawString($"{emp.Name} ({emp.TotalHours:F1}h) ({(int)(emp.TotalHours / total * 100)}%)", new Font("Arial", 10),
                    Brushes.Black, 545, (int)startAngle / 2 + 50);
                startAngle += sweepAngle;
            }

            bmp.Save(filePath);


            Console.WriteLine($" Pie Chart saved: {Path.GetFullPath(filePath)}");
        }
    }
}
