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
        public static async Task Main(string[] args)
        {
            //랜덤 연결코드 생성
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            string password = new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray());
            
            //GUI 호출
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var gui = new GUI(password);

            //GUI 실행
            Thread t = new Thread(() => {
                Application.Run(gui);
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            gui.UpdateTextBox(">> Connection code: " + password);
            //Console.WriteLine(password);
            
            while (true)
            {
                //get uri 생성
                var getUri = "http://localhost:8080/api/userInfo/";
                getUri += password;


                using (var client = new HttpClient())
                {
                    try
                    {
                        gui.UpdateTextBox(">> getUri: " + getUri);
                        //Console.WriteLine(getUri);
                        var response = await client.GetAsync(getUri);
                        var content = await response.Content.ReadAsStringAsync();

                        // Deserialize the JSON string into a dynamic object
                        dynamic obj1 = JsonConvert.DeserializeObject(content);

                        gui.UpdateTextBox(obj1);
                        Console.WriteLine(obj1);
                        // Access the individual properties of the object and print them
                        gui.UpdateTextBox(">> Surveying: " + obj1.flag);
                        //Console.WriteLine("Flag: " + obj1.flag);

                        while (obj1.flag == true)
                        {
                            connector = new Connector();
                            connector.DeviceConnected += new EventHandler(OnDeviceConnected);
                            connector.DeviceConnectFail += new EventHandler(OnDeviceFail);
                            connector.DeviceValidating += new EventHandler(OnDeviceValidating);

                            // Scan for devices across COM ports
                            // The COM port named will be the first COM port that is checked.
                            connector.ConnectScan("COM6");


                            // Blink detection needs to be manually turned on
                            connector.setBlinkDetectionEnabled(true);
                            while (true)
                            {
                                response = await client.GetAsync(getUri);
                                content = await response.Content.ReadAsStringAsync();
                                obj1 = JsonConvert.DeserializeObject(content);
                                if (obj1.flag == false)
                                {
                                    drawChart form1 = new drawChart();

                                    Series seriesA = new Series("Attention");
                                    Series seriesM = new Series("Meditation");


                                    // Chart를 Line Chart로 설정합니다.
                                    seriesA.ChartType = SeriesChartType.Line;

                                    for (int i = 0; i < 4; i++)
                                    {
                                        if (attention.Count > 0)
                                        {
                                            var val = attention[0];
                                            attention.RemoveAt(0);
                                        }
                                        if (meditation.Count > 0)
                                        {
                                            var val = meditation[0];
                                            meditation.RemoveAt(0);
                                        }
                                    }

                                    foreach (object obj in attention)
                                    {
                                        seriesA.Points.Add((double)obj);
                                    }

                                    foreach (object obj in meditation)
                                    {
                                        seriesM.Points.Add((double)obj);
                                    }

                                    form1.chart1.Series.Add(seriesA);
                                    form1.chart1.Series.Add(seriesM);

                                    // Chart를 Line Chart로 설정합니다.
                                    seriesA.ChartType = SeriesChartType.Line;
                                    seriesM.ChartType = SeriesChartType.Line;

                                    form1.chart1.SaveImage("C:\\Users\\USER\\Desktop\\NeuroSky MindWave Mobile_Example_HelloEEG\\chart.png", ChartImageFormat.Png);

                                    gui.UpdateTextBox(">> Connection closed.");
                                    //System.Console.WriteLine("Goodbye.");
                                    connector.Close();

                                    // POST
                                    var postUri = new Uri("http://localhost:8080/api/imgInfo");

                                    var data = new { memberId = obj1.memberId, surveyId = obj1.surveyId, code = obj1.code };

                                    var imageContent = new ByteArrayContent(File.ReadAllBytes("C:\\Users\\USER\\Desktop\\NeuroSky MindWave Mobile_Example_HelloEEG\\chart.png"));
                                    imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");

                                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                                    var jsonContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
                                    gui.UpdateTextBox(jsonData);
                                    Console.WriteLine(jsonData);

                                    var mergedContent = new MultipartFormDataContent();
                                    mergedContent.Add(jsonContent, "braindata");
                                    mergedContent.Add(imageContent, "image", "image.png");
                                    
                                    //Console.WriteLine(mergedContent);
                                    //Console.WriteLine(mergedContent.Headers);
                                    var response2 = await client.PostAsync(postUri, mergedContent);
                                    var result = await response2.Content.ReadAsStringAsync();
                                }
                                Thread.Sleep(2000);
                            }


                        }


                    }
                    catch { Thread.Sleep(5000); continue; }


                }




            }
            
        }


        // Called when a device is connected 

        static void OnDeviceConnected(object sender, EventArgs e)
        {
            Connector.DeviceEventArgs de = (Connector.DeviceEventArgs)e;

            Console.WriteLine("Device found on: " + de.Device.PortName);
            de.Device.DataReceived += new EventHandler(OnDataReceived);
        }




        // Called when scanning fails

        static void OnDeviceFail(object sender, EventArgs e)
        {
            Console.WriteLine("No devices found! :(");
        }



        // Called when each port is being validated

        static void OnDeviceValidating(object sender, EventArgs e)
        {

            Console.WriteLine("Validating: ");

        }


        // Called when data is received from a device

        static void OnDataReceived(object sender, EventArgs e)
        {

            //Device d = (Device)sender;

            Device.DataEventArgs de = (Device.DataEventArgs)e;
            DataRow[] tempDataRowArray = de.DataRowArray;

            TGParser tgParser = new TGParser();

            tgParser.Read(de.DataRowArray);

            /* Loops through the newly parsed data of the connected headset*/
            // The comments below indicate and can be used to print out the different data outputs. 

            for (int i = 0; i < tgParser.ParsedData.Length; i++)
            {

                if (tgParser.ParsedData[i].ContainsKey("Raw"))
                {

                    //Console.WriteLine("Raw Value:" + tgParser.ParsedData[i]["Raw"]);

                }

                if (tgParser.ParsedData[i].ContainsKey("PoorSignal"))
                {

                    //The following line prints the Time associated with the parsed data
                    //Console.WriteLine("Time:" + tgParser.ParsedData[i]["Time"]);

                    //A Poor Signal value of 0 indicates that your headset is fitting properly
                    Console.WriteLine("Poor Signal:" + tgParser.ParsedData[i]["PoorSignal"]);

                    poorSig = (byte)tgParser.ParsedData[i]["PoorSignal"];
                }


                if (tgParser.ParsedData[i].ContainsKey("Attention"))
                {

                    Console.WriteLine("Att Value:" + tgParser.ParsedData[i]["Attention"]);
                    attention.Add(tgParser.ParsedData[i]["Attention"]);

                }


                if (tgParser.ParsedData[i].ContainsKey("Meditation"))
                {

                    Console.WriteLine("Med Value:" + tgParser.ParsedData[i]["Meditation"]);
                    meditation.Add(tgParser.ParsedData[i]["Meditation"]);

                }


                if (tgParser.ParsedData[i].ContainsKey("EegPowerDelta"))
                {

                    //Console.WriteLine("Delta: " + tgParser.ParsedData[i]["EegPowerDelta"]);

                }

                if (tgParser.ParsedData[i].ContainsKey("BlinkStrength"))
                {

                    //Console.WriteLine("Eyeblink " + tgParser.ParsedData[i]["BlinkStrength"]);

                }


            }

        }

    }

}
