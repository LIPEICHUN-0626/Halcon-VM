using System;
using System.ComponentModel;

namespace HalconWinFormsDemo.Models
{
    public sealed class VmRoiLayer : INotifyPropertyChanged, IDisposable
    {
        private string name;
        private bool isEnabled;
        private bool isVisible;
        private bool isLocked;
        private RoiData geometry;
        private string bindingSummary;
        private string contextResultCode;
        private string contextResultText;
        private int sequence;

        public VmRoiLayer()
        {
            RoiId = Guid.NewGuid().ToString("N");
            Name = "ROI";
            IsEnabled = true;
            IsVisible = true;
            BindingSummary = "未绑定工具";
            ContextResultCode = "--";
            ContextResultText = "当前工具未运行";
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

        public bool IsLocked
        {
            get { return isLocked; }
            set
            {
                if (isLocked == value)
                {
                    return;
                }

                isLocked = value;
                OnPropertyChanged("IsLocked");
                OnPropertyChanged("LockText");
                OnPropertyChanged("StateText");
            }
        }

        public string LockText
        {
            get { return IsLocked ? "已锁定" : "可编辑"; }
        }

        public RoiData Geometry
        {
            get { return geometry; }
            set
            {
                if (ReferenceEquals(geometry, value))
                {
                    return;
                }

                geometry = value;
                NotifyGeometryChanged();
            }
        }

        public string ShapeText
        {
            get { return Geometry == null ? "--" : Geometry.ShapeDisplayText; }
        }

        public string GeometryText
        {
            get { return Geometry == null ? "--" : Geometry.DisplayText; }
        }

        public string StateText
        {
            get { return (IsEnabled ? "参与运行" : "已停用") + " · " + LockText; }
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

        public string ContextResultCode
        {
            get { return contextResultCode; }
            set
            {
                string resolved = string.IsNullOrWhiteSpace(value) ? "--" : value;
                if (contextResultCode == resolved)
                {
                    return;
                }

                contextResultCode = resolved;
                OnPropertyChanged("ContextResultCode");
            }
        }

        public string ContextResultText
        {
            get { return contextResultText; }
            set
            {
                string resolved = string.IsNullOrWhiteSpace(value) ? "当前工具未运行" : value;
                if (contextResultText == resolved)
                {
                    return;
                }

                contextResultText = resolved;
                OnPropertyChanged("ContextResultText");
            }
        }

        public void Dispose()
        {
            if (geometry != null)
            {
                geometry.Dispose();
                geometry = null;
            }
        }

        public void ReplaceGeometry(RoiData replacement)
        {
            if (replacement == null)
            {
                throw new ArgumentNullException("replacement");
            }

            RoiData previous = geometry;
            geometry = replacement;
            if (previous != null)
            {
                previous.Dispose();
            }
            NotifyGeometryChanged();
        }

        private void NotifyGeometryChanged()
        {
            OnPropertyChanged("Geometry");
            OnPropertyChanged("ShapeText");
            OnPropertyChanged("GeometryText");
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
            get { return Layer == null ? "--" : Layer.StateText + " · " + (Layer.IsVisible ? "可见" : "隐藏"); }
        }

        public string SequenceText
        {
            get { return Layer == null ? "--" : Layer.SequenceText; }
        }

        public string RoiIdText
        {
            get
            {
                string value = RoiId;
                return value.Length <= 8 ? value : value.Substring(0, 8);
            }
        }

        public string GeometryText
        {
            get { return Layer == null ? "--" : Layer.GeometryText; }
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

    public sealed class VmRoiRunResult
    {
        public string RoiId { get; set; }

        public string RoiName { get; set; }

        public string ShapeText { get; set; }

        public string ResultCode { get; set; }

        public string ValueName { get; set; }

        public double Value { get; set; }

        public string Message { get; set; }

        public string ErrorMessage { get; set; }

        public double ElapsedMilliseconds { get; set; }

        public string ValueText
        {
            get { return ValueName + "=" + Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture); }
        }

        public string ElapsedText
        {
            get { return ElapsedMilliseconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ms"; }
        }

        public string DisplayMessage
        {
            get { return string.IsNullOrWhiteSpace(ErrorMessage) ? Message : ErrorMessage; }
        }
    }
}
