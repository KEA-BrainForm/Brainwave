using System;

using System.Text;
using System.Threading;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

using NeuroSky.ThinkGear;
using System.Collections;
using HelloEEG;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Linq;
using System.Windows.Forms;



namespace testprogram 
{
    
    class Program
    {
        static ArrayList attention = new ArrayList();
        static ArrayList meditation = new ArrayList();

        static Connector connector;
        static byte poorSig;
        [STAThread]
        public static void Main(string[] args)
        {
            //랜덤 연결코드 생성
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            string password = new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray());
            
            
            //GUI 호출
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Thread thread = new Thread(() =>
            {
                Application.Run(new GUI(password));
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

        }
    }

}
