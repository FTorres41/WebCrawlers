﻿using RSBM.Controllers;
using RSBM.Models;
using RSBM.Repository;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RSBM
{
    public partial class RService : ServiceBase
    {

        public static Dictionary<string, Timer> schedulars;
        private enum commands
        {
            exeNowBEC = 255, exeNowBECPE = 254,
            exeNowTCE = 253, exeNowCMG = 252,
            exeNowBB = 251, exeNowCNET = 250,
            exeNowBBRM = 249, exeNowGROBOT = 248,
            exeNowCNETHT = 247, exeNowCNETCotacao = 246,
            exeNowGELEMENT = 245, exeNowTCMCE = 244,
            exeNowTCEPI = 243, exeNowTCESE = 242,
            exeNowTCERS = 240,
            exeNowCRJ = 239,
            exeNowCNETPrecos = 237
        }

        public RService()
        {
            InitializeComponent();
        }

#if DEBUG
        public void StartDebug(string[] args)
        {
            OnStart(args);
        }
#endif

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            Log("RService OnSessionChange: {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
            if (changeDescription.Reason == SessionChangeReason.SessionLogon)
            {
                ScheduleAll();
            }
        }

        protected override void OnStart(string[] args)
        {
            Log("RService started/restarted: {0}", Path.GetTempPath() + "RSERVICE" + ".txt");

#if (DEBUG)
            {
                //BBController.InitCallBack(new object());
                //BBRMController.RemainingCallBack(new object());
                //CNETController.InitCallBack(new object());
                //CNETHTController.HistoricCallBack(new object());
                //CMGController.InitCallBack(new object());
                //BECController.InitCallBack(new object());
                //BECPEController.InitCallBack(new object());
                //CNETCotacaoController.InitCallBack(new object());
                //TCEPRController.InitCallBack(new object());
                //TCMCEController.InitCallBack(new object());
                TCEPIController.InitCallBack(new object());
                //TCESEController.InitCallBack(new object());
                //CRJController.InitCallBack(new object());
                //CNETPrecosController.InitCallBack(new object());
            }
#else
            {
                ScheduleAll();
            }
#endif
        }

        private void ScheduleAll()
        {
            schedulars = new Dictionary<string, Timer>();
            schedulars.Add(BECController.Name, new Timer(new TimerCallback(BECController.InitCallBack)));
            schedulars.Add(CMGController.Name, new Timer(new TimerCallback(CMGController.InitCallBack)));
            schedulars.Add(BECPEController.Name, new Timer(new TimerCallback(BECPEController.InitCallBack)));
            schedulars.Add(TCEPRController.Name, new Timer(new TimerCallback(TCEPRController.InitCallBack)));
            schedulars.Add(BBController.Name, new Timer(new TimerCallback(BBController.InitCallBack)));
            schedulars.Add(BBRMController.Name, new Timer(new TimerCallback(BBRMController.RemainingCallBack)));
            schedulars.Add(CNETController.Name, new Timer(new TimerCallback(CNETController.InitCallBack)));
            schedulars.Add(CNETHTController.Historic, new Timer(new TimerCallback(CNETHTController.HistoricCallBack)));
            schedulars.Add(CNETCotacaoController.Name, new Timer(new TimerCallback(CNETCotacaoController.InitCallBack)));
            schedulars.Add(TCMCEController.Name, new Timer(new TimerCallback(TCMCEController.InitCallBack)));
            schedulars.Add(TCEPIController.name, new Timer(new TimerCallback(TCEPIController.InitCallBack)));
            schedulars.Add(TCESEController.name, new Timer(new TimerCallback(TCESEController.InitCallBack)));
            schedulars.Add(CRJController.Name, new Timer(new TimerCallback(CRJController.InitCallBack)));
            schedulars.Add(CNETPrecosController.Name, new Timer(new TimerCallback(CNETPrecosController.InitCallBack)));
            ScheduleService();
        }

        /*Método para receber comandos customizados de fora do serviço*/
        protected override void OnCustomCommand(int command)
        {
            base.OnCustomCommand(command);
            switch (command)
            {
                case (int)commands.exeNowBEC:
                    ExeNow(BECController.Name);
                    break;
                case (int)commands.exeNowBECPE:
                    ExeNow(BECPEController.Name);
                    break;
                case (int)commands.exeNowTCE:
                    ExeNow(TCEPRController.Name);
                    break;
                case (int)commands.exeNowCMG:
                    ExeNow(CMGController.Name);
                    break;
                case (int)commands.exeNowBB:
                    ExeNow(BBController.Name);
                    break;
                case (int)commands.exeNowCNET:
                    ExeNow(CNETController.Name);
                    break;
                case (int)commands.exeNowBBRM:
                    ExeNow(BBRMController.Name);
                    break;
                case (int)commands.exeNowGROBOT:
                    ExeNow(GenericRobotController.Name);
                    break;
                case (int)commands.exeNowCNETHT:
                    ExeNow(CNETHTController.Historic);
                    break;
                case (int)commands.exeNowCNETCotacao:
                    ExeNow(CNETCotacaoController.Name);
                    break;
                case (int)commands.exeNowGELEMENT:
                    ExeNow(GenericElementController.Name);
                    break;
                case (int)commands.exeNowTCMCE:
                    ExeNow(TCMCEController.Name);
                    break;
                case (int)commands.exeNowTCEPI:
                    ExeNow(TCEPIController.name);
                    break;
                case (int)commands.exeNowTCESE:
                    ExeNow(TCESEController.name);
                    break;
                case (int)commands.exeNowTCERS:
                    ExeNow(TCERSController.Name);
                    break;
                case (int)commands.exeNowCRJ:
                    ExeNow(CRJController.Name);
                    break;
                case (int)commands.exeNowCNETPrecos:
                    ExeNow(CNETPrecosController.Name);
                    break;
            }
        }

        /*Executado no OnStart do serviço para agendar a execução dos robots de acordo com os dados do bd*/
        public static void ScheduleService()
        {
            try
            {
                foreach (var schedular in schedulars)
                {
                    //Get from database mode from this key (example = "BEC")
                    ConfigRobot config = ConfigRobotController.FindByName(schedular.Key);
                    DateTime scheduleTime = DateTime.MinValue;

                    if (config.Mode.ToUpper().Equals(ConfigRobotController.Interval))
                    {
                        //Get from database interval from this key (example = "BEC")
                        int intervalMin = (int)config.IntervalMin;
                        scheduleTime = DateTime.Now.AddMinutes(intervalMin);
                    }
                    else if (config.Mode.ToUpper().Equals(ConfigRobotController.Daily))
                    {
                        //Get from database daily hour from this key (example = "BEC")
                        int hour = (int)config.ScheduleTime;

                        if (DateTime.Now.Hour >= hour)
                        {
                            DateTime now = DateTime.Now.AddDays(1);
                            scheduleTime = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0);
                        }
                        else
                            scheduleTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, 0, 0);
                    }

                    TimeSpan timeSpan = scheduleTime.Subtract(DateTime.Now);

                    config.NextDate = scheduleTime;
                    config.Status = 'W';

                    /*Log*/
                    string schedule = string.Format("{0} day(s) {1} hours {2} minutes", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes);
                    Log("RService schedule " + config.Name + " to run after: " + schedule + " {0}", Path.GetTempPath() + config.Name + ".txt");

                    long dueTime = Convert.ToInt64(timeSpan.TotalMilliseconds);
                    schedular.Value.Change(dueTime, Timeout.Infinite);

                    ConfigRobotRepository repo = new ConfigRobotRepository();
                    repo.Update(config);
                }
            }
            catch (Exception e)
            {
                Log("RService Error on: {0} " + e.Message + e.StackTrace, Path.GetTempPath() + "RSERVICE" + ".txt");
                using (var service = new ServiceController("RService"))
                {
                    service.Stop();
                }
            }
        }

        /*Chamado no fim da execução de cada robot para agendar a próxima execução do robo que chamou.*/
        public static void ScheduleMe(ConfigRobot config)
        {
            try
            {
                DateTime scheduleTime = DateTime.MinValue;

                if (config.Mode.ToUpper().Equals(ConfigRobotController.Interval))
                {
                    int intervalMin = (int)config.IntervalMin;
                    scheduleTime = DateTime.Now.AddMinutes(intervalMin);
                }
                else if (config.Mode.ToUpper().Equals(ConfigRobotController.Daily))
                {
                    //Get from database daily hour from this key (example = "BEC")
                    int hour = (int)config.ScheduleTime;
                    if (DateTime.Now.Hour >= hour)
                    {
                        DateTime now = DateTime.Now.AddDays(1);
                        scheduleTime = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0);
                    }
                    else
                        scheduleTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, 0, 0);
                }

                TimeSpan timeSpan = scheduleTime.Subtract(DateTime.Now);

                config.NextDate = scheduleTime;
                config.Status = 'W';
                /*Log*/
                string schedule = string.Format("{0} day(s) {1} hours {2} minutes", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes);
                Log("RService schedule " + config.Name + " to run after: " + schedule + " {0}", Path.GetTempPath() + config.Name + ".txt");

                long dueTime = Convert.ToInt64(timeSpan.TotalMilliseconds);
                schedulars[config.Name].Change(dueTime, Timeout.Infinite);

                ConfigRobotRepository repo = new ConfigRobotRepository();
                repo.Update(config);
            }
            catch (Exception e)
            {
                Log("RService Error on " + config.Name + ": {0} " + e.Message + e.StackTrace, Path.GetTempPath() + "RSERVICE" + ".txt");
            }
        }

        /*Executa o robo em 1 minuto a partir de agora*/
        public static void ExeNow(string name)
        {
            try
            {
                TimeSpan timeSpan = DateTime.Now.AddMinutes(1).Subtract(DateTime.Now);

                /*Log*/
                string schedule = string.Format("{0} day(s) {1} hours {2} minutes", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes);
                Log("RService schedule " + name + " to run after: " + schedule + " {0}", Path.GetTempPath() + name + ".txt");

                long dueTime = Convert.ToInt64(timeSpan.TotalMilliseconds);
                schedulars[name].Change(dueTime, Timeout.Infinite);
            }
            catch (Exception e)
            {
                Log("RService Error on: {0} " + e.Message + e.StackTrace, Path.GetTempPath() + "RSERVICE" + ".txt");
            }
        }

        /*Log de eventos*/
        public static void Log(string v, string path)
        {
            try
            {
                using (StreamWriter st = new StreamWriter(path, true))
                {
                    st.WriteLine(string.Format(v, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss tt")));
                }
            }
            catch (Exception e)
            {
            }
        }

        //public static void KillTask(string name)
        //{
        //    try
        //    {
        //        Process[] tasks = Process.GetProcessesByName(name);
        //        foreach (var task in tasks)
        //        {
        //            task.Kill();
        //        }
        //    }
        //    catch(Exception e)
        //    {
        //        Log("Exception (KillTask " + name + ") RService: " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
        //    }
        //}

        protected override void OnStop()
        {
            using (var service = new ServiceController("RService"))
            {
                if (service.Status != ServiceControllerStatus.Running && service.Status != ServiceControllerStatus.Stopped)
                {
                    service.Start();
                    var args = new string[2] { null, null };
                    OnStart(args);
                }
            }

            Log("RService stopped: {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
        }

        protected override void OnPause()
        {
            using (var service = new ServiceController("RService"))
            {
                if (service.Status != ServiceControllerStatus.Running && service.Status != ServiceControllerStatus.Stopped)
                {
                    service.Start();
                    var args = new string[2] { null, null };
                    OnStart(args);
                }
            }
        }
    }
}