using NeuroSky.ThinkGear;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections;
using System.Security.Authentication.ExtendedProtection;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace HelloEEG
{
    public partial class GUI : Form
    {
        static ArrayList attention = new ArrayList();
        static double sumAttention = 0;
        static ArrayList meditation = new ArrayList();
        static double sumMeditation = 0;
        static ArrayList blink = new ArrayList();

        static ArrayList blinkTimes = new ArrayList(); // blink 값이 추가된 시간을 저장하는 리스트
        static bool isBlinkTrue = false; // blink 값이 2초 내에 3번 이상 추가되었는지를 나타내는 변수

        static Connector connector;
        static byte poorSig;
        private string password;

        public GUI(string password)
        {
            InitializeComponent();
            this.password = password;
            pw.Text = password;
            _ = Brain();
        }


        public async Task<dynamic> Brain()
        {
            UpdateTextBox("Connecting code: " + password);
            //get uri 생성
            var getUri = "http://localhost:8080/api/userInfo/";
            getUri += password;
            UpdateTextBox("getUri: " + getUri);

            using (HttpClient client = new HttpClient())
            {
                while (true)
                {
                    try
                    {
                        var response = await client.GetAsync(getUri);
                        await Task.Delay(3000);
                        var content = await response.Content.ReadAsStringAsync();
                        // Deserialize the JSON string into a dynamic object
                        dynamic obj1 = JsonConvert.DeserializeObject(content);

                        Console.WriteLine(obj1);
                        // Access the individual properties of the object and print them
                        UpdateTextBox("Surveying: " + obj1.flag);
                        Console.WriteLine("Flag: " + obj1.flag);

                        if (obj1.flag == true)
                        {
                            connector = new Connector();
                            connector.DeviceConnected += new EventHandler(OnDeviceConnected);
                            connector.DeviceConnectFail += new EventHandler(OnDeviceFail);
                            connector.DeviceValidating += new EventHandler(OnDeviceValidating);
                            // Scan for devices across COM ports
                            // The COM port named will be the first COM port that is checked.
                            connector.ConnectScan("COM4");

                            // Blink detection needs to be manually turned on
                            connector.setBlinkDetectionEnabled(true);

                            while (true)
                            {
                                // flag를 받아오기 위함: false->true면 설문 시작이었고, true->false면 설문 종료하기 위함
                                response = await client.GetAsync(getUri);
                                content = await response.Content.ReadAsStringAsync();
                                obj1 = JsonConvert.DeserializeObject(content);

                                if (isBlinkTrue)
                                {
                                    obj1.flag = false;
                                    UpdateTextBox("2초 내에 3회 이상 눈을 깜빡여서 설문이 종료되었습니다.");
                                }

                                if (obj1.flag == false)
                                {

                                    drawChart form1 = new drawChart();

                                    Series seriesA = new Series("Attention");
                                    Series seriesM = new Series("Meditation");


                                    // Chart를 Line Chart로 설정
                                    seriesA.ChartType = SeriesChartType.Line;
                                    seriesM.ChartType = SeriesChartType.Line; ;


                                    // 처음 측정을 시작하면 4개정도 0으로 받아지므로 앞부분 4개 삭제
                                    for (int i = 0; i < 4; i++)
                                    {
                                        if (attention.Count > 0) { attention.RemoveAt(0); }
                                        if (meditation.Count > 0) { meditation.RemoveAt(0); }
                                    }

                                    foreach (object obj in attention) { seriesA.Points.Add((double)obj); }
                                    foreach (object obj in meditation) { seriesM.Points.Add((double)obj); }

                                    // 집중도, 안정도 평균 구하기
                                    double avgAttention = sumAttention / attention.Count;
                                    double avgMeditation = sumMeditation / meditation.Count;
                                    System.Console.WriteLine(avgAttention);

                                    form1.chart1.Series.Add(seriesA);
                                    form1.chart1.Series.Add(seriesM);
                                    string strPath = AppDomain.CurrentDomain.BaseDirectory;
                                    DirectoryInfo di = new DirectoryInfo(strPath);
                                    if (di.Exists == false) { di.Create(); }
                                    System.Console.WriteLine(strPath);
                                    form1.chart1.SaveImage(strPath + @"\chart.png", ChartImageFormat.Png);

                                    UpdateTextBox("Connection closed. Bye.");
                                    System.Console.WriteLine("Goodbye.");
                                    connector.Close();

                                    // POST
                                    var postUri = new Uri("http://localhost:8080/api/imgInfo");

                                    var data = new { memberId = obj1.memberId, surveyId = obj1.surveyId, code = obj1.code, avgAtt = avgAttention, avgMed = avgMeditation };

                                    var imageContent = new ByteArrayContent(File.ReadAllBytes(strPath + @"\chart.png"));
                                    imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");

                                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                                    var jsonContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
                                    Console.WriteLine(jsonData);

                                    var mergedContent = new MultipartFormDataContent();
                                    mergedContent.Add(jsonContent, "braindata");
                                    mergedContent.Add(imageContent, "image", "image.png");

                                    //Console.WriteLine(mergedContent);
                                    //Console.WriteLine(mergedContent.Headers);
                                    var response2 = await client.PostAsync(postUri, mergedContent);
                                    var result = await response2.Content.ReadAsStringAsync();

                                    await Task.Delay(99999999);
                                    //Environment.Exit(0);
                                }
                            }

                        }
                    }
                    catch
                    {
                        UpdateTextBox("Trying to connect...");
                        continue;
                    }
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
            NeuroSky.ThinkGear.DataRow[] tempDataRowArray = de.DataRowArray;

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
                    //Console.WriteLine("Poor Signal:" + tgParser.ParsedData[i]["PoorSignal"]);

                    if (tgParser.ParsedData[i]["PoorSignal"] > 50)
                    {
                        Console.WriteLine("Poor SIGNAL!");
                    }

                    poorSig = (byte)tgParser.ParsedData[i]["PoorSignal"];
                }


                if (tgParser.ParsedData[i].ContainsKey("Attention"))
                {
                    Console.WriteLine("Att Value:" + tgParser.ParsedData[i]["Attention"]);

                    attention.Add(tgParser.ParsedData[i]["Attention"]);
                    sumAttention += tgParser.ParsedData[i]["Attention"];
                }


                if (tgParser.ParsedData[i].ContainsKey("Meditation"))
                {
                    Console.WriteLine("Med Value:" + tgParser.ParsedData[i]["Meditation"]);

                    meditation.Add(tgParser.ParsedData[i]["Meditation"]);
                    sumMeditation += tgParser.ParsedData[i]["Meditation"];
                }


                if (tgParser.ParsedData[i].ContainsKey("EegPowerDelta"))
                {
                    //Console.WriteLine("Delta: " + tgParser.ParsedData[i]["EegPowerDelta"]);
                }

                if (tgParser.ParsedData[i].ContainsKey("BlinkStrength"))
                {
                    Console.WriteLine("Eyeblink " + tgParser.ParsedData[i]["BlinkStrength"]);

                    blink.Add(tgParser.ParsedData[i]["BlinkStrength"]);

                    // 현재 시간을 blinkTimes 리스트에 추가
                    blinkTimes.Add(DateTime.Now);

                    // 2초 이내에 3번 이상 blink 값이 추가되었는지 확인
                    if (blinkTimes.Count >= 3)
                    {
                        TimeSpan diff = (DateTime)blinkTimes[blinkTimes.Count - 1] - (DateTime)blinkTimes[blinkTimes.Count - 3];
                        if (diff.TotalSeconds <= 2)
                        {
                            // 2초 이내에 3번 이상 blink 값이 추가되었음
                            System.Console.WriteLine("2초 내에 3회 이상 눈을 깜빡여서 설문이 종료되었습니다.");
                            isBlinkTrue = true;
                        }
                    }
                }


            }

        }
        private void gui_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {


            // 클립보드에 접근하여 비동기로 복사 작업 실행
            Clipboard.SetText(password);
            // 복사 작업이 완료되면 메시지를 보여줌
            MessageBox.Show("연결 코드가 클립보드에 복사되었습니다.\n뇌파 측정 기기와의 연결을 위해 웹페이지에 코드를 입력해주세요!");
            UpdateTextBox("Connection code copied to clipboard");
        }

        private void logBox_TextChanged(object sender, EventArgs e)
        {

        }

        public void UpdateTextBox(string text)
        {
            logBox.Text += ">> " + text + Environment.NewLine;
        }
    }
}
