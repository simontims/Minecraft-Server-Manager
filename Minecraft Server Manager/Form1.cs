﻿using System;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Globalization;

namespace Minecraft_Server_Manager
{
    public partial class Form1 : Form
    {

        Process minecraftProcess;
        StreamWriter mcInputStream;
     
        string players = "Loading Player List...";
        string players2 = "";

        private System.Threading.Timer timer;

        public delegate void fpTextBoxCallback_t(string strText);
        public fpTextBoxCallback_t fpTextBoxCallback;    
        public string logFile = "serverlog.log";

        public void sayDelay(int sayTimer, params string[] sayThis)
        {
            Task.Delay(sayTimer * 1000).ContinueWith(t => serverSay(sayThis));
        }

        public void serverSay(params string[] toSay)
        {
            foreach (string say in toSay)
            mcInputStream.WriteLine("say " + say);
        }

        public string replaceString(String s, String r)
        {
            int start = s.IndexOf("{");
            int end = s.IndexOf("}", start);
            string result = s.Substring(start, (end - start) +1);
            return s = s.Replace(result, r);
        }



        public Form1()
        {
            string startServer = ConfigurationManager.AppSettings["startServer"].ToString();
            string automaticBackups = ConfigurationManager.AppSettings["automaticBackups"].ToString();
            string date = ConfigurationManager.AppSettings["dateTime"].ToString();

            CheckForIllegalCrossThreadCalls = false;
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            fpTextBoxCallback = new fpTextBoxCallback_t(AddTextToOutputTextBox);
            InitializeComponent();

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = 5000;           
            timer.Enabled = true;                       
            timer.Start();

            dateTimePicker1.Text = date;
            List<string> weatherList = new List<string>();
            weatherList.Add("clear");
            weatherList.Add("rain");
            weatherList.Add("thunder");

            List<string> gameRuleList = new List<string>();
            gameRuleList.Add("commandblockoutput");
            gameRuleList.Add("commandblocksenabled");
            gameRuleList.Add("dodaylightcycle");
            gameRuleList.Add("doentitydrops");
            gameRuleList.Add("dofiretick");
            gameRuleList.Add("doimmediaterespawn");
            gameRuleList.Add("doinsomnia");
            gameRuleList.Add("domobloot");
            gameRuleList.Add("domobspawning");
            gameRuleList.Add("dotiledrops");
            gameRuleList.Add("doweathercycle");
            gameRuleList.Add("drowningdamage");
            gameRuleList.Add("falldamage");
            gameRuleList.Add("firedamage");
            //gameRuleList.Add("functioncommandlimit");
            gameRuleList.Add("keepinventory");
            //gameRuleList.Add("maxcommandchainlength");
            gameRuleList.Add("mobgriefing");
            gameRuleList.Add("naturalregeneration");
            gameRuleList.Add("pvp");
            //gameRuleList.Add("randomtickspeed");
            gameRuleList.Add("sendcommandfeedback");
            gameRuleList.Add("showcoordinates");
            gameRuleList.Add("showdeathmessages");
            gameRuleList.Add("tntexplodes");

            if (startServer == "true")
            {
                startServerCheckbox.Checked = true;
            }

            if (automaticBackups == "true")
            {
                automaticBackupsCheckBox.Checked = true;
                SetUpTimer();
            }

            textBoxLoginMsg.Text = ConfigurationManager.AppSettings["loginMsg"].ToString();
            textBoxWelcomeMsg.Text = ConfigurationManager.AppSettings["welcomeMsg"].ToString();

            weatherComboBox.DataSource = weatherList;
            gameRuleComboBox.DataSource = gameRuleList;

            if(startServer == "true")
            {
                startServerButton_Click(null, EventArgs.Empty);
            }           
        } // End Constructor

        int restartTryLimit = 0;

        
        private void SetUpTimer()
        {
            TimeSpan alertTime = dateTimePicker1.Value.TimeOfDay;
            DateTime current = DateTime.Now;
            TimeSpan timeToGo = alertTime - current.TimeOfDay;
            if (timeToGo < TimeSpan.Zero)
            {
                return;//time already passed
            }
            this.timer = new System.Threading.Timer(x =>
            {
                backgroundWorker1.RunWorkerAsync();
            }, null, timeToGo, Timeout.InfiniteTimeSpan);
        }
        public void AddTextToOutputTextBox(string strText)
        {  
            string blah = strText.Replace("\r\n", "");

            try
            {
                if (blah.Contains("CrashReporter") && restartTryLimit < 3)
                {
                    Thread.Sleep(5000);
                    startServerButton_Click(null, EventArgs.Empty);
                    restartTryLimit++;
                    return;
                }
                if (blah.Contains("Server Started."))
                {
                    restartTryLimit = 0;
                }
                if (blah.Contains("Network port occupied, can't start server."))
                {
                    this.txtOutput.AppendText(strText);
                    txtOutput.ScrollToCaret();
                    File.AppendAllText(logFile, strText);
                    return;
                }
                if (blah.Contains("Version"))
                {
                    string thisVersion = blah.Substring(blah.LastIndexOf(' ') + 1);
                    statusLabel.Text = "Running: " + thisVersion;
                }
                if (blah.Contains("commandblock") && blah.Contains("="))
                {
                    string removeComma = blah.Replace(", ", "\r\n");
                    gameRulesTxt.Text = removeComma;
                    return;
                }
                if (strText.Contains("players online"))
                {
                   strText = strText.Replace("\r\n", "");
                }
                if (blah.Contains(", xuid") || blah.Contains(", port"))
                {
                    this.txtOutput.AppendText(strText);
                    txtOutput.ScrollToCaret();
                    File.AppendAllText(logFile,strText);
                    // xuid when a player connects or disconnects, also refresh players list

                    if (blah.Contains("Player connected"))
                    {
                    // Get the player name
                    int pFrom = blah.IndexOf("connected: ") + "connected: ".Length;
                    int pTo = blah.LastIndexOf(", ");
                    string playerName = blah.Substring(pFrom, pTo - pFrom);
                    this.txtOutput.AppendText("\r\n[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "SRVR] Player name:" + playerName);
                    txtOutput.ScrollToCaret();

                    string loginMsg = textBoxLoginMsg.Text.ToString();
                    string welcomeMsg = textBoxWelcomeMsg.Text.ToString();

                    if (loginMsg.Contains("{playerName}"))
                    {
                        loginMsg = replaceString(loginMsg, playerName);
                    }
                    if (welcomeMsg.Contains("{playerName}"))
                    {
                        welcomeMsg = replaceString(welcomeMsg, playerName);
                    }

                    this.txtOutput.AppendText("\r\n" + loginMsg.ToString());
                    this.txtOutput.AppendText("\r\n" + welcomeMsg.ToString());
                    txtOutput.ScrollToCaret();
                    sayDelay(10, loginMsg.ToString(), welcomeMsg.ToString());
                    }

                }
                else if (strText.Contains("players online"))
                {                    
                    players = players + strText + "\r\n";
                }               

                else if (strText.Contains("players online") || (!blah.Contains(" ") && strText.Length > 0) || blah.Contains(", ") )
                {
                    string removeCR = strText.Replace("\r\n","");
                    removeCR = removeCR.Replace(" ", "");
                    string[] names = removeCR.Split(',');                 
                    Array.Sort(names);
                    string result = string.Join("\r\n", names);                 
                    players = players + result;
                }
                else
                {
                    this.txtOutput.AppendText(strText);
                    txtOutput.ScrollToCaret();
                    File.AppendAllText(logFile, strText);
                }              
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }           
        } 

        private void ConsoleOutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                if (this.InvokeRequired)
                    this.Invoke(fpTextBoxCallback, Environment.NewLine + outLine.Data);
                else
                    fpTextBoxCallback(Environment.NewLine + outLine.Data);
            }           
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.minecraftProcess.HasExited)
                {
                    txtOutput.AppendText("\r\n\r\nThe server has been shutdown.\r\n");
                    txtOutput.ScrollToCaret();
                    File.AppendAllText(logFile, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "The server has been shutdown.\r\n");
                    return;
                }
                mcInputStream.WriteLine(txtInputCommand.Text);
            }
            catch
            {

            }
        }


        public void ProcessExited(object sender, EventArgs e)
        {
            txtOutput.AppendText("\r\n\r\nThe server has been shutdown.\r\n");
            txtOutput.ScrollToCaret();
            File.AppendAllText(logFile, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "The server has been shutdown.\r\n");
        }


        private void backupButton_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void startServerButton_Click(object sender, EventArgs e)
        {
            string processFileName = "";

            processFileName = @"bedrock_server.exe";
            if (!File.Exists(processFileName))
            {
                AddTextToOutputTextBox("bedrock_server.exe not found\r\n");
            }
            else
            {
                minecraftProcess = new System.Diagnostics.Process();

                minecraftProcess.StartInfo.FileName = processFileName;

                AddTextToOutputTextBox("Using this terminal: " + minecraftProcess.StartInfo.FileName);

                minecraftProcess.StartInfo.UseShellExecute = false;
                minecraftProcess.StartInfo.CreateNoWindow = true;
                minecraftProcess.StartInfo.RedirectStandardInput = true;
                minecraftProcess.StartInfo.RedirectStandardOutput = true;
                minecraftProcess.StartInfo.RedirectStandardError = true;

                minecraftProcess.EnableRaisingEvents = true;
                minecraftProcess.Exited += new EventHandler(ProcessExited);
                minecraftProcess.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(ConsoleOutputHandler);
                minecraftProcess.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(ConsoleOutputHandler);

                minecraftProcess.Start();

                mcInputStream = minecraftProcess.StandardInput;
                minecraftProcess.BeginOutputReadLine();
                minecraftProcess.BeginErrorReadLine();

                mcInputStream.WriteLine("gamerule");

                startServerButton.Enabled = false;
                stopServerButton.Enabled = true;
            }
        }

        private void stopServerButton_Click(object sender, EventArgs e)
        {
            try
            {
                mcInputStream.WriteLine("stop");
                playerTxtOutput.Clear();
                gameRulesTxt.Clear();
                startServerButton.Enabled = true;
                stopServerButton.Enabled = false;
                statusLabel.Text = "Stopped";
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void OnProcessExit(object sender, EventArgs e)
        {           
            // Should check if server is running before sending this command
            mcInputStream.WriteLine("stop");
            Thread.Sleep(5000);
        }

        private void weatherComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void setWeatherButton_Click(object sender, EventArgs e)
        {
            try
            {
                mcInputStream.WriteLine("weather " + weatherComboBox.SelectedItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void opPlayerButton_Click(object sender, EventArgs e)
        {
            try
            {
                mcInputStream.WriteLine("op " + opPlayerTextBox1.Text.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void deOpPlayerButton_Click(object sender, EventArgs e)
        {
            try
            {
                mcInputStream.WriteLine("deop " + deOpTextBox1.Text.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string trueFalse = " false";
            if (trueGRRadioButton.Checked)
                trueFalse = " true";
            if (falseGRRadioButton2.Checked)
                trueFalse = " false";
            try
            {
                mcInputStream.WriteLine("gamerule " + gameRuleComboBox.SelectedItem + trueFalse);
                mcInputStream.WriteLine("gamerule ");
            }
            catch
            {

            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (players != players2)
                {
                    playerTxtOutput.Clear();
                    playerTxtOutput.Text = players;
                }
                mcInputStream.WriteLine("list");
                players = players2;
            }
            catch
            {

            }
        }

        private void txtInputCommand_KeyDown(object sender, KeyEventArgs e)
        {           
            if (e.KeyCode == Keys.Enter)
                btnExecute_Click(sender, e);        
        }

        private void weatherComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                setWeatherButton_Click(sender, e);
        }

        private void opPlayerTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                opPlayerButton_Click(sender, e);
        }

        private void deOpTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                deOpPlayerButton_Click(sender, e);
        }

        private void gameRuleComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                button1_Click(sender, e);
        }

        private void trueGRRadioButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                button1_Click(sender, e);
        }

        private void falseGRRadioButton2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                button1_Click(sender, e);
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            mcInputStream.WriteLine("say THE SERVER IS GOING DOWN FOR A BACKUP IN 10 SECONDS");
            txtOutput.AppendText("\r\n\r\nTelling players the server is going down in 10 seconds\r\n");
            txtOutput.ScrollToCaret();
            File.AppendAllText(logFile, "\r\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "Telling players the server is going down in 10 seconds\r\n");

            Thread.Sleep(10000);
            txtOutput.AppendText("\r\nStopping Server\r\n");
            txtOutput.ScrollToCaret();
            File.AppendAllText(logFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "Stopping Server\r\n");
            mcInputStream.WriteLine("stop");
            Thread.Sleep(5000);
            statusLabel.Text = "Stopped";

            startServerButton.Enabled = true;
            stopServerButton.Enabled = false;

            string source_dir = "";
            string destination_dir = "";

            source_dir = @"worlds";
            destination_dir = @"backups\worlds" + DateTime.Now.ToString("hhmmttMMddyyyy");


            txtOutput.AppendText("\r\nStarting Backup\r\n\r\n");
            txtOutput.ScrollToCaret();
            File.AppendAllText(logFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "Starting Backup\r\n");
            foreach (string dir in System.IO.Directory.GetDirectories(source_dir, "*", System.IO.SearchOption.AllDirectories))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(destination_dir, dir.Substring(source_dir.Length + 1)));               
            }

            foreach (string file_name in System.IO.Directory.GetFiles(source_dir, "*", System.IO.SearchOption.AllDirectories))
            {
                System.IO.File.Copy(file_name, System.IO.Path.Combine(destination_dir, file_name.Substring(source_dir.Length + 1)));
                txtOutput.AppendText("Backing up: " + file_name + "    TO:    " + destination_dir + file_name + "\r\n\r\n");
                File.AppendAllText(logFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "Backing up: " + file_name + "    TO:    " + destination_dir + file_name + "\r\n");
                txtOutput.ScrollToCaret();

            }
            Thread.Sleep(5000);
            txtOutput.AppendText("\r\nBackup Complete. Starting server\r\n\r\n");
            txtOutput.ScrollToCaret();
            File.AppendAllText(logFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "Backup Complete. Starting server\r\n");

            startServerButton_Click(sender, e);            
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);            
            configuration.Save(ConfigurationSaveMode.Modified);
            if (startServerCheckbox.Checked)
            {
                configuration.AppSettings.Settings["startServer"].Value = "true";
                configuration.Save(ConfigurationSaveMode.Modified);
            }
            if (!startServerCheckbox.Checked)
            {                
                configuration.AppSettings.Settings["startServer"].Value = "false";
                configuration.Save(ConfigurationSaveMode.Modified);
            }
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            SetUpTimer();
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.Save(ConfigurationSaveMode.Modified);
            configuration.AppSettings.Settings["dateTime"].Value = dateTimePicker1.Value.TimeOfDay.ToString();
            configuration.Save(ConfigurationSaveMode.Modified);
        }

        private void automaticBackupsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.Save(ConfigurationSaveMode.Modified);
            if (automaticBackupsCheckBox.Checked)
            {
                configuration.AppSettings.Settings["automaticBackups"].Value = "true";
                configuration.Save(ConfigurationSaveMode.Modified);
            }
            if (!automaticBackupsCheckBox.Checked)
            {
                configuration.AppSettings.Settings["automaticBackups"].Value = "false";
                configuration.Save(ConfigurationSaveMode.Modified);
            }
        }

        private void textBoxLoginMsg_TextChanged(object sender, EventArgs e)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings["loginMsg"].Value = textBoxLoginMsg.Text.ToString();
            configuration.Save(ConfigurationSaveMode.Modified);
        }

        private void textBoxWelcomeMsg_TextChanged(object sender, EventArgs e)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings["welcomeMsg"].Value = textBoxWelcomeMsg.Text.ToString();
            configuration.Save(ConfigurationSaveMode.Modified);
        }

    }
}






