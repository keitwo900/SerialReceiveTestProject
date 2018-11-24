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
using System.Threading;

namespace SerialReceiveTest
{
	public partial class Form1 : Form
	{
		const int RX_BUFFER_SIZE = 1024; //2のべき乗とすること！
		char[] tmpBuf; //受信一時バッファ
		char[] ringBuf; //受信データ保持バッファ

		int ri; //リードインデクス
		int wi; //ライトインデクス

		//データ処理関連
		delegate void ShowDataDelegate();
		ShowDataDelegate showDataDelegate;

		//スレッド関連
		bool isThreadRun;
		Thread dataRxThread;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public Form1()
		{
			InitializeComponent();

			tmpBuf = new char[RX_BUFFER_SIZE];
			ringBuf = new char[RX_BUFFER_SIZE];
			serialPort1.Encoding = Encoding.UTF8; //エンコーディング指定

			showDataDelegate = new ShowDataDelegate(ShowData); //デリゲートの関数登録
			dataRxThread = new Thread(new ThreadStart(DataReceive)); //スレッドの関数登録
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
		// スレッド
		//-------------------------------------------------------------------
		//-------------------------------------------------------------------

		/// <summary>
		/// 受信データ処理
		/// </summary>
		void DataReceive()
		{
			while(isThreadRun)
			{
				BeginInvoke(showDataDelegate); //データをこの関数の中で処理しきるならBeginInvoke。この関数内で処理したデータを、while内のこの関数の後で使用したいならInvokeに変更。
				Thread.Sleep(1);
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

			//受信データ処理スレッド開始
			dataRxThread.Start();
			isThreadRun = true;
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

		/// <summary>
		/// アプリ終了前処理
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			isThreadRun = false; //threadのwhile loopを抜ける
			dataRxThread.Join(); //threadの終了待ち
			dataRxThread = null;
		}
	}
}
