using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CNC.Core.Comands;

namespace ioSenderTouch.ViewModels
{
   public class VirtualKeyboardViewModel : INotifyPropertyChanged
    {
        private string _textFromKeyBoard;
        public ICommand FunctionKeys { get; }
        public ICommand CharKeys { get; }

        public string TextFromKeyBoard
        {
            get => _textFromKeyBoard;
            set
            {
                if (value == _textFromKeyBoard) return;
                _textFromKeyBoard = value;
                OnPropertyChanged();
            }
        }

        public VirtualKeyboardViewModel()
        {
            FunctionKeys = new Command(SetFunctionKeys);
            CharKeys = new Command(SetCharKeys);
        }
        private void SetCharKeys(object charKey)
        {
            TextFromKeyBoard += charKey;
        }

        private void SetFunctionKeys(object funcKey)
        {
            switch (funcKey.ToString())
            {
                case "LEFT":
                    break; 
                case "RIGHT":
                    break;
                case "ENTER":
                    TextFromKeyBoard += Environment.NewLine;
                    break;
                case "DELETE":
                    if (string.IsNullOrEmpty(TextFromKeyBoard)) return;
                    var delete = TextFromKeyBoard.Remove(0, 1);
                    TextFromKeyBoard = delete;
                    break;
                case "BACKSPACE":
                    if(string.IsNullOrEmpty(TextFromKeyBoard))return;
                    var remove = TextFromKeyBoard.Remove(TextFromKeyBoard.Length - 1, 1);
                    TextFromKeyBoard = remove;
                    break;
                case "SPACE":
                    TextFromKeyBoard += " ";
                    break;


            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
                //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
