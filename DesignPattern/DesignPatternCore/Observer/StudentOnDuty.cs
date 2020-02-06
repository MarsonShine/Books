using System;
using System.Collections.Generic;

namespace DesignPatternCore.Observer {
    public class StudentOnDuty : ISubject {
        public Action UpdateEvent { get; set; }
        private List<StudentObserver> observers = new List<StudentObserver>();
        public void Notify() {
            foreach (var observer in observers) {
                observer.Update();
            }
        }

        public void Attach(StudentObserver observer) {
            observers.Add(observer);
        }

        public string State => "班主任来了！！！";
    }
}