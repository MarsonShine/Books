using System;

namespace DesignPatternCore.Builder {
    public class DefaultFullComputerBuilder : AbstractFullComputerBuilder {
        protected override void SetCPU() {

        }

        protected override void SetDisk() {

        }

        protected override void SetDisplay() {

        }

        protected override void SetGraphics() {

        }

        protected override void SetMainboard() {

        }
    }
    public abstract class AbstractFullComputerBuilder : IFullComputerBuilder {
        public string Mainboard { get; set; } = "默认品牌主板";
        public string CPU { get; set; } = "默认品牌CPU";
        public string Disk { get; set; } = "默认品牌内存";
        public string Graphics { get; set; } = "默认品牌显卡";
        public string Display { get; set; } = "默认品牌显示器";
        public bool HasOperatingSystem { get; set; }

        public IFullComputer Create() {
            SetMainboard();
            SetCPU();
            SetDisk();
            SetDisplay();
            SetGraphics();
            InstallOperatingSystem();
            if (!HasOperatingSystem) throw new InvalidOperationException("install faild: no operating system");
            return this;
        }

        protected abstract void SetMainboard();
        protected abstract void SetCPU();
        protected abstract void SetDisk();
        protected abstract void SetGraphics();
        protected abstract void SetDisplay();

        private void InstallOperatingSystem() {
            //if (!condition) return;
            HasOperatingSystem = true;
        }
    }
}