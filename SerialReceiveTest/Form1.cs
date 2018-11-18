using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace SerialReceiveTest
{
	public partial class Form1 : Form
	{
		const int RX_BUFFER_SIZE = 1024;
		char[] tmpBuf; //受信一時バッファ
		char[] ringBuf; //受信データ保持バッファ

		int ri; //リードインデクス
		int wi; //ライトインデクス

		delegate void ShowDataDelegate();

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public Form1()
		{
			InitializeComponent();

			tmpBuf = new char[RX_BUFFER_SIZE];
			ringBuf = new char[RX_BUFFER_SIZE];
			serialPort1.Encoding = Encoding.UTF8; //エンコーディング指定
		}

		/// <summary>
		/// シリアルポートを取得して、ComboBoxのリストに追加します。
		/// </summary>
		void GetComName()
		{
			string[] ports = SerialPort.GetPortNames(); //ポート取得

			//ComboBoxに追加
			foreach (string port in ports)
			{
				comboBox_ComName.Items.Add(port);
			}
		}

		/// <summary>
		/// 受信データをテキストボックスに表示します。
		/// 改行はCR+LFみ受け付けます。
		/// </summary>
		void ShowData()
		{
			while(ri != wi)
			{
				textBox_ReceiveMsg.Text += ringBuf[ri++];
				ri &= RX_BUFFER_SIZE - 1; //リミッタ
			}
		}

		//-------------------------------------------------------------------
		//-------------------------------------------------------------------
		// 以下、フォームのイベント関連
		//-------------------------------------------------------------------
		//-------------------------------------------------------------------

		/// <summary>
		/// フォームロード
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Form1_Load(object sender, EventArgs e)
		{
			GetComName();

			//COMポートが1つでも存在した場合は、最初に検出したCOMポートをデフォルトでセットする
			if (comboBox_ComName.Items.Count != 0)
			{
				comboBox_ComName.Text = comboBox_ComName.Items[0].ToString();
			}
		}

		/// <summary>
		/// ポート名更新ボタンが押されたとき
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button_PortUpdate_Click(object sender, EventArgs e)
		{
			GetComName();
		}

		/// <summary>
		/// 接続ボタンが押されたとき
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button_Connect_Click(object sender, EventArgs e)
		{
			if (button_Connect.Text == "接続") //ポートオープン
			{
				if (comboBox_ComName.Text == "")
				{
					MessageBox.Show("ポートを選択してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				serialPort1.PortName = comboBox_ComName.Text;

				try
				{
					serialPort1.Open();
					button_Connect.Text = "切断";
				}
				catch (System.UnauthorizedAccessException ex) //別プロセスがすでに指定したCOMを開いている場合など
				{
					MessageBox.Show(ex.ToString());
				}
			}
			else //ポートクローズ
			{
				serialPort1.Close();
				button_Connect.Text = "接続";
			}
		}

		/// <summary>
		/// シリアルポート　データ受信イベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			int count = serialPort1.Read(tmpBuf, 0, tmpBuf.Length); //一時バッファにデータを受信

			//リングバッファにデータを格納
			int i;
			for (i = 0; i < count; i++)
			{
				ringBuf[wi++] = tmpBuf[i];
				wi &= RX_BUFFER_SIZE - 1; //リミッタ
			}

			BeginInvoke(new ShowDataDelegate(ShowData));
		}

		/// <summary>
		/// メッセージクリア
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button_MsgClear_Click(object sender, EventArgs e)
		{
			textBox_ReceiveMsg.Clear();
		}
	}
}
