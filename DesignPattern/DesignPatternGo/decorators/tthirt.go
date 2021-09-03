package decorators

type TThirts struct {
	finery Finery
	name   string
}

func (t TThirts) Show() string {
	if t.finery != nil {
		return t.name + "、" + t.finery.Show()
	}
	return t.name
}

func NewTThirts(f Finery) TThirts {
	return TThirts{
		name:   "白衬衫",
		finery: f,
	}
}
