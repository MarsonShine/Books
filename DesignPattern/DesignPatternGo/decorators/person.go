package decorators

type Person struct {
	name   string
	finery Finery
}

func (p Person) Print() {
	print(p.name + " 穿着 " + p.finery.Show())
}

func NewPerson(name string, f Finery) Person {
	return Person{
		name:   name,
		finery: f,
	}
}
