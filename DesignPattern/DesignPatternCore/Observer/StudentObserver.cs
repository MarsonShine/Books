using System;

namespace DesignPatternCore.Observer {
    public class StudentObserver {
        private readonly StudentOnDuty _studentOnDuty;
        public StudentObserver(string studentTypeName, StudentOnDuty studentOnDuty) {
            _studentOnDuty = studentOnDuty;
            StudentTypeName = studentTypeName;
        }
        public void Update() {
            Console.WriteLine(StudentTypeName + "接受到了来自值日生的通知：" + _studentOnDuty.State + " 关闭讲台的电脑并假装翻开书本看书，写作业等");
        }

        public string StudentTypeName { get; }
    }
}