using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace RFID_Scan_Monitor
{

    public partial class Form1 : Form
    {
        LinkedList<string> rawlist = new LinkedList<string>();
        LinkedList<LinkedListNode<RFTag>> taglist = new LinkedList<LinkedListNode<RFTag>>();
        LinkedList<int> histogram = new LinkedList<int>();
        SaveFileDialog logFile;
        StreamWriter sw,sw2;
        string cutoff = String.Empty;
        private readonly object listLock = new object();
        StringBuilder sb = new StringBuilder();
        char LF = (char)10;


        bool headerRead = true;
        public Form1()
        {
            InitializeComponent();
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            Font replacementFont = new Font("Arial", 30, FontStyle.Bold);
            IDs.Font = replacementFont;
            replacementFont = new Font("Arial", 20, FontStyle.Bold);
            label2.Font = replacementFont;
            getports();  
        }


         
        void getports()
        {
            String[] ports = System.IO.Ports.SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
        }
        LinkedListNode<RFTag> getTag(LinkedList<LinkedListNode<RFTag>> list,string TagID)
        {
            if (list.Any() == false) 
            { 
                return null;
            }
            TagID = TagID.Trim('\n');
            foreach (LinkedListNode<RFTag> tag in list)
            {
                if (TagID.Contains(tag.Value.EPC))
                {
                    return tag;
                }
            }
            return null;
        }
        private void PrintTagList (LinkedList<LinkedListNode<RFTag>> list)
        {
            string currentDate = DateTime.Today.ToString("ddMMyyyy");
            logFile = new SaveFileDialog();
            logFile.FileName = "Rf_Log_" + currentDate + ".txt";
            logFile.Filter = "Text Files | *.txt";
            if (logFile.ShowDialog() == DialogResult.OK)
            {
                sw2 = new StreamWriter(logFile.FileName);
                AddToListBox("Printing Tag List: Size =" + list.Count);
                sw2.WriteLine("Printing Tag List: Size =" + list.Count);
                AddToListBox("EPC                                             Total#\t\tA1\tA2\tA3");
                sw2.WriteLine("EPC                           Total#\tA1\tA2\tA3");
                if (list.Any() == false)
                {
                    return;
                }
                foreach (LinkedListNode<RFTag> tag in list)
                {
                    AddToListBox(tag.Value.EPC + "\t" + tag.Value.readcount + "\t" + tag.Value.antcount[0] + "\t" + tag.Value.antcount[1] + "\t" + tag.Value.antcount[2]);
                    sw2.Write(tag.Value.EPC + "\t" + tag.Value.readcount + "\t" + tag.Value.antcount[0] + "\t" + tag.Value.antcount[1] + "\t" + tag.Value.antcount[2]);
                }
                sw2.Close();
            }
            return;
        }
        private void AddToListBox(string oo)
        {
            Invoke(new MethodInvoker(
                           delegate { listBox1.Items.Add(oo); }
                           ));
        }
        private void AddToListBox2(string oo)
        {
            Invoke(new MethodInvoker(
                           delegate { listBox2.Items.Add(oo); }
                           ));
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }



        // This event handler is where the time-consuming work is done.
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int i = 0;
            int hist;
            while (true) 
            { 
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    // Perform a time consuming operation and report progress.
                    //System.Threading.Thread.Sleep(50);
                    lock (listLock)
                    {
                        if (rawlist.Any())
                        {
                            string s = rawlist.First();
                            rawlist.RemoveFirst();
                            s = s.Trim('\n');
                            
                                var data = s.Split(new[] { ',' });
                                if (data.Length == 8)
                                {
                                    AddToListBox2(data[0] + data[1] + data[2] + data[3] + data[4] + data[5] + data[6] + data[7]);

                                    LinkedListNode<RFTag> T;
                                    T = getTag(taglist, data[0]);
                                    if (T == null)
                                    {
                                        RFTag tag = new RFTag(data[0], 0, 0, Int32.Parse(data[3]), 0, 0, 0, "0");
                                        tag.antcount[Int32.Parse(data[2]) - 1] = Int32.Parse(data[3]);
                                        T = new LinkedListNode<RFTag>(tag);
                                        taglist.AddLast(T);
                                        i++;
                                        hist = int.Parse(DateTime.Now.ToString("hhmmss"));
                                        hist += (Int32.Parse(data[2])-1) * 1000000;
                                        hist += Int32.Parse(data[3]) * 10000000;
                                        if (checkBox1.Checked)
                                        {
                                            hist += 100000000;
                                        }
                                        histogram.AddLast(hist);
                                        worker.ReportProgress(i);

                                    }
                                    else
                                    {
                                        T.Value.readcount += Int32.Parse(data[3]);
                                        T.Value.antcount[Int32.Parse(data[2]) - 1] += Int32.Parse(data[3]);
                                    }
                                    cutoff = String.Empty;
                                }
                        }
                    }
                }
            }
        }

        // This event handler updates the progress.
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            IDs.Text = e.ProgressPercentage.ToString();
        }

        // This event handler deals with the results of the background operation.
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                label4.Text = "Tags Sorted";
            }
            else if (e.Error != null)
            {
                label4.Text = "Error: " + e.Error.Message;
            }
            else
            {
                resultLabel.Text = "Done Processing!";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string currentDate = DateTime.Today.ToString("ddMMyyyy");

            logFile = new SaveFileDialog();
            logFile.FileName = "Rf_Log_Raw_" + currentDate + ".txt";
            logFile.Filter = "Text Files | *.txt";

            if (logFile.ShowDialog() == DialogResult.OK)
            {
                if (!headerRead)
                {
                    resultLabel.Text = "Started Scan: Waiting for Header";
                    AddToListBox2("Started Scan: Waiting for Header");
                    AddToListBox2("");
                }
                else
                {
                    resultLabel.Text = "Started Scan: Skipping Header";
                    AddToListBox2("Started Scan: Skipping Header");
                    AddToListBox2("");
                }
                button2.Enabled = true;
                button1.Enabled = false;
                sw = new StreamWriter(logFile.FileName);

                if (backgroundWorker1.IsBusy != true)
                {
                    // Start the asynchronous operation.
                    backgroundWorker1.RunWorkerAsync();
                    label4.Text = "Processing Data";
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            headerRead = false;
            SerialPort.Close();
            sw.Close();
            PrintTagList(taglist);

            resultLabel.Text = "Stopped";
            button1.Enabled = true;
            button3.Enabled = true;
            if (backgroundWorker1.WorkerSupportsCancellation == true)
            {
                // Cancel the asynchronous operation.
                backgroundWorker1.CancelAsync();
            }
        }

        private void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            System.IO.Ports.SerialPort sp = (System.IO.Ports.SerialPort)sender;
            string s;
            string indata = sp.ReadExisting();

            if (sw != null)
            {
                sw.Write(indata);
            }
            if (headerRead)
            {

                foreach (char c in indata)
                {
                    if (c == LF)
                    {
                        sb.Append(c);

                        s = sb.ToString();
                        sb.Clear();
                        lock (listLock)
                        {
                            rawlist.AddLast(s);
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            else
            {
                AddToListBox2(indata);
            }
            if (!headerRead && indata.Contains("PHASE, mV"))
            {
                headerRead = true;
                Invoke(new MethodInvoker(
                           delegate { resultLabel.Text = "Started Scan: Header Found"; }
                           ));
                AddToListBox2("Header Found");
                AddToListBox2("");
                AddToListBox2("Incoming Tags");
            }
           
        }

        private void button3_Click(object sender, EventArgs e)
        {

            try 
            {
                if (comboBox1.Text == "" || numericUpDown1.Value == 0)
                {
                    resultLabel.Text = "Please select port settings";
                }
                else
                {
                    SerialPort.PortName = comboBox1.Text;
                    SerialPort.BaudRate = (int)numericUpDown1.Value;
                    resultLabel.Text = "Connected";
                    AddToListBox2("Connected");
                    SerialPort.Open();
                    button3.Enabled = false;
                    button1.Enabled = true;
                }
            }

            catch (Exception ex)
            {
                resultLabel.Text = "Connection Error: " + ex.Message;
            }
           
        }


        private void button5_Click(object sender, EventArgs e)
        {
            string currentDate = DateTime.Today.ToString("ddMMyyyy");

            logFile = new SaveFileDialog();
            logFile.FileName = "Rf_Histogram" + currentDate + ".txt";
            logFile.Filter = "Text Files | *.txt";

            if (logFile.ShowDialog() == DialogResult.OK)
            {
                StreamWriter w = new StreamWriter(logFile.FileName);
                foreach (int time in histogram)
                {
                    w.WriteLine(time);
                }
                w.Close();
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            listBox2.Items.Clear();
        }
    }
    class RFTag
    {
        public string EPC;
        public int protocol;
        public int antenna;
        public int readcount;
        public int RSSI;
        public int freq;
        public int phase;
        public string mv;
        public int[] antcount = new int[3];
        public RFTag(string id, int prot, int ant, int cnt, int rs, int fre, int pha, string v)
        {
            EPC = id;
            protocol = prot;
            antenna = ant;
            readcount = cnt;
            RSSI = rs;
            freq = fre;
            phase = pha;
            mv = v;
            
        }
    }

}
