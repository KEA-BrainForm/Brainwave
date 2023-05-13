using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HelloEEG
{
    public partial class GUI : Form
    {
        private string password;
        public GUI(string password)
        {
            InitializeComponent();
            this.password = password;
            pw.Text = password;
        }

        private void gui_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // password 변수에 저장된 값을 클립보드에 복사합니다.
            Clipboard.SetText(password);

            // 복사가 완료되었다는 메시지를 출력합니다.
            MessageBox.Show("연결 코드가 클립보드에 복사되었습니다.\n뇌파 측정 기기와의 연결을 위해 웹페이지에 코드를 입력해주세요!");
            UpdateTextBox(">> Connection code copied to clipboard");
        }

        private void logBox_TextChanged(object sender, EventArgs e)
        {

        }

        public void UpdateTextBox(string text)
        {
            logBox.Text += text + Environment.NewLine;
        }

    }
}
