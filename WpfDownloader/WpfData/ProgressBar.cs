using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfDownloader.WpfData
{
    public class ProgressBar : INotifyPropertyChanged
    {
        private double value;

        public double Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                NotifyPropertyChanged("value");
            }
        }

        public ProgressBar()
        {
            
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string info)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
            }));

        }

    }
}
