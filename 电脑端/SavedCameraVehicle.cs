using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Gqqnbig.Lego
{
    class SavedCameraVehicle : CameraVehicleBase, IEnumerable<TimedEventArgs>
    {
        ///// <summary>
        ///// 还没有读取过的文件索引。
        ///// </summary>
        //private int nextFileIndex = 0;

        //private readonly string[] fileNames;

        int nextEventIndex;
        //readonly List<MotorSpeedChangedEventArgs> actionList;

        readonly TimedEventArgs[] timedEvents;

        private SavedCameraVehicle(TimedEventArgs[] timedEvents)
        {
            this.timedEvents = timedEvents;
        }

        public SavedCameraVehicle(string directory)
        {
            var fileNames = Directory.GetFiles(directory, "*.jpg", SearchOption.TopDirectoryOnly);

            Array.Sort(fileNames);

            var imageAvailableEventList = fileNames.Select(f => new ImageAvailableEventArgs
             {
                 Tick = Convert.ToInt64(Path.GetFileNameWithoutExtension(f)),
                 Image = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(f)
             });

            List<MotorSpeedChangedEventArgs> actionList;
            if (File.Exists(Path.Combine(directory, "actions.xml")))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<MotorSpeedChangedEventArgs>));

                using (FileStream stream = new FileStream(Path.Combine(directory, "actions.xml"), FileMode.Open))
                {
                    actionList = (List<MotorSpeedChangedEventArgs>)serializer.Deserialize(stream);
                }
                actionList.Sort((x, y) => (int)(x.Tick - y.Tick));
            }
            else
                actionList = new List<MotorSpeedChangedEventArgs>();

            timedEvents = imageAvailableEventList.Cast<TimedEventArgs>().Concat(actionList).OrderBy(e => e.Tick).ToArray();
            nextEventIndex = 0;
        }

        public bool HasNextEvent()
        {
            return nextEventIndex < timedEvents.Length;
        }

        public void RaiseNextEvent()
        {
            if (HasNextEvent() == false)
                throw new InvalidOperationException("当HasNextEvent()返回true时才能调用RaiseNextEvent()。");

            TimedEventArgs te = timedEvents[nextEventIndex++];
            var ie = te as ImageAvailableEventArgs;
            if (ie != null)
                OnImageAvailable(ie);
            else
                OnMotorSpeedChanged((MotorSpeedChangedEventArgs)te);
        }

        public override void Dispose()
        { }

        public IEnumerator<TimedEventArgs> GetEnumerator()
        {
            return timedEvents.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return timedEvents.GetEnumerator();
        }
    }
}