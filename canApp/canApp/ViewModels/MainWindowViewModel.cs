﻿using System.IO.Ports;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using canApp.Models;
using ReactiveUI; 
using canApp.Services;

namespace canApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ICommand RefreshComPortsCommand { get; }
    public ICommand ConnectToComCommand { get; }
    public ICommand SendUserMessage { get; }
    public ICommand SendCanParams { get; }
    public ICommand ResetRequest { get; }
    public ICommand ClearReceivedData { get; }
    public ICommand ReadSpeed { get; }
    public ComViewModel ComList { get; }
    private readonly SerialPort _serial = new SerialPort();

    public MainWindowViewModel(DebugComList cp)
    {
        ComList = new ComViewModel(cp.GetItems());

        SendCanParams = ReactiveCommand.Create(() =>
        {
            try
            {
                SendMasterCmd(1, SelectedSpeed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        });
        ResetRequest = ReactiveCommand.Create(() =>
        {
            try
            {
                SendMasterCmd(0, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        });
        ReadSpeed = ReactiveCommand.Create(() =>
        {
            try
            {
                SendMasterCmd(2, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        });
        
        RefreshComPortsCommand = ReactiveCommand.Create(() =>
        {
            string[] serialPortList = SerialPort.GetPortNames();
            try
            {
                ComList.Items.Clear();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            foreach (var port in serialPortList)
            {
                ComList.Items.Add(new ComPort() { Name = port });
            }
        });
        
        SendUserMessage = ReactiveCommand.Create(() =>
        {
            if (IsConnected)
            {
                try
                {
                    WriteToSerial(UserInput);
                    var dat = DateTime.Now.ToString("HH:mm:ss.ffff");
                    SerialData += $"==> TX<{dat}> ~ {UserInput}\n";
                    UserInput = "";
                }
                catch (Exception)
                {
                    SerialData += "~~~Failed to send data!~~~\n";

                }
            }
        });     
            
        ClearReceivedData = ReactiveCommand.Create(() =>
        {
            SerialData = "";
        });

        ConnectToComCommand = ReactiveCommand.Create(() =>
        {
            if (ConnectionButton == "Connect")
            {
                _serial.PortName = ComList.Items.ElementAt(SelectedCom).Name;
                _serial.BaudRate = 115200;
                _serial.Handshake = System.IO.Ports.Handshake.None;
                _serial.Parity = Parity.None;
                _serial.DataBits = 8;
                _serial.StopBits = StopBits.Two;
                _serial.ReadTimeout = 200;
                _serial.WriteTimeout = 50;
                try
                {
                    _serial.Open();
                    IsConnected = _serial.IsOpen;
                    if (IsConnected == true)
                    {
                        StatusColor = "Green";
                    }
                    SerialData += "~~~Connected Successful!~~~\n";
                    Thread.Sleep(10);
                    SendMasterCmd(2, 0);
                    _serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(Receive);
                    ConnectionButton = "Disconnect";


                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

            }
            else
            {
                try
                {
                    _serial.Close();
                    IsConnected = _serial.IsOpen;
                    if (IsConnected == false)
                    {
                        StatusColor = "Red";
                    }

                    ConnectionButton = "Connect";

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        });

    }

    private void WriteToSerial(string msg)
    {
        byte[] hexstring = Encoding.ASCII.GetBytes(msg += '\n');
        foreach (byte hexValue in hexstring)
        {
            byte[] _hexValue = new byte[] { hexValue };
            _serial.Write(_hexValue, 0, 1);
            Thread.Sleep(1);
        }
    }
    private void SendMasterCmd(int cmd, int value)
    {
        WriteToSerial("FF" + cmd.ToString() + value.ToString());
    }

    private void Receive(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
    {
        try
        {
            var date = DateTime.Now.ToString("HH:mm:ss.ffff");
            string message = _serial.ReadLine().Trim('\r', '\n');
            if (message.Length == 32)
            {
                string message_Id = message.Substring(0,4);
                switch (message_Id)
                {
                    case "0002":
                        int.TryParse(message.Substring(5,2), out int message_cmd);
                        int.TryParse(message.Substring(8,2), out int message_val);
                        if (message_cmd == 1 && message_val >= 0 && message_val <= 3)
                            SelectedSpeed = message_val;
                        break;
                    default:
                        break;
                }
                
                SerialData += $"<== Rx<{date}> ~ {message}\n";
            }
            
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        
    }

}
