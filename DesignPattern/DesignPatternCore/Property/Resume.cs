using System;

namespace DesignPatternCore.Property {
    public class Resume : ICloneable {
        private string name;
        private string sex;
        private string age;

        private WorkExperience workExperience;
        public Resume(string name) {
            this.name = name;
            workExperience = new WorkExperience();
        }

        private Resume(WorkExperience workExperience) {
            this.workExperience = (WorkExperience) workExperience.Clone();
        }

        public void SetPersonalInfo(string sex, string age) {
            this.sex = sex;
            this.age = age;
        }

        public void SetWorkExperience(string timeArea, string company) {
            workExperience.Company = company;
            workExperience.WorkDate = timeArea;
        }

        public void Display() {
            Console.WriteLine($"{name} {sex} {age}");
            Console.WriteLine($"工作经历：{workExperience.WorkDate} {workExperience.Company}");
        }
        public object Clone() {
            // return this.MemberwiseClone(); // 值对象复制值，引用对象复制的是对象的引用。不适用于含有引用对象的对象（string除外，它是特殊的引用类型）
            var r = new Resume(this.workExperience);
            r.SetPersonalInfo(this.sex, this.age);
            return r;
        }
    }
}