namespace DesignPatternCore.Visitor.Version1
{
    public abstract class Element {
        public abstract void Accept(IVisitor visitor);
    }

    public abstract class ConCreateEmentA : Element
    {
        public override void Accept(IVisitor visitor)
        {
            visitor.Visitor(this);
        }
    }
}