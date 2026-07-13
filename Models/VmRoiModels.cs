using System;
using System.ComponentModel;

namespace HalconWinFormsDemo.Models
{
    public sealed class VmRoiLayer : INotifyPropertyChanged, IDisposable
    {
        private string name;
        private bool isEnabled;
        private bool isVisible;
        private string bindingSummary;
        private int sequence;

        public VmRoiLayer()
        {
            RoiId = Guid.NewGuid().ToString("N");
            Name = "ROI";
            IsEnabled = true;
            IsVisible = true;
            BindingSummary = "未绑定工具";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string RoiId { get; set; }

        public int Sequence
        {
            get { return sequence; }
            set
            {
                if (sequence == value)
                {
                    return;
                }

                sequence = value;
                OnPropertyChanged("Sequence");
                OnPropertyChanged("SequenceText");
            }
        }

        public string SequenceText
        {
            get { return Sequence.ToString("00"); }
        }

        public string Name
        {
            get { return name; }
            set
            {
                string resolved = string.IsNullOrWhiteSpace(value) ? "ROI" : value.Trim();
                if (name == resolved)
                {
                    return;
                }

                name = resolved;
                OnPropertyChanged("Name");
            }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled == value)
                {
                    return;
                }

                isEnabled = value;
                OnPropertyChanged("IsEnabled");
                OnPropertyChanged("StateText");
            }
        }

        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (isVisible == value)
                {
                    return;
                }

                isVisible = value;
                OnPropertyChanged("IsVisible");
            }
        }

        public RoiData Geometry { get; set; }

        public string ShapeText
        {
            get { return Geometry == null ? "--" : Geometry.ShapeType.ToString(); }
        }

        public string GeometryText
        {
            get { return Geometry == null ? "--" : Geometry.DisplayText; }
        }

        public string StateText
        {
            get { return IsEnabled ? "参与运行" : "已停用"; }
        }

        public string BindingSummary
        {
            get { return bindingSummary; }
            set
            {
                bindingSummary = string.IsNullOrWhiteSpace(value) ? "未绑定工具" : value;
                OnPropertyChanged("BindingSummary");
            }
        }

        public void Dispose()
        {
            if (Geometry != null)
            {
                Geometry.Dispose();
                Geometry = null;
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public sealed class VmRoiBindingItem : INotifyPropertyChanged
    {
        private bool isBound;

        public event PropertyChangedEventHandler PropertyChanged;

        public VmRoiLayer Layer { get; set; }

        public string RoiId
        {
            get { return Layer == null ? string.Empty : Layer.RoiId; }
        }

        public string Name
        {
            get { return Layer == null ? "--" : Layer.Name; }
        }

        public string ShapeText
        {
            get { return Layer == null ? "--" : Layer.ShapeText; }
        }

        public string StateText
        {
            get { return Layer == null ? "--" : Layer.StateText; }
        }

        public bool IsBound
        {
            get { return isBound; }
            set
            {
                if (isBound == value)
                {
                    return;
                }

                isBound = value;
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs("IsBound"));
                }
            }
        }
    }
}
