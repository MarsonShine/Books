package decorators

type BigTrouser struct {
	finery Finery
	name   string
}

func (bt BigTrouser) Show() string {
	return bt.name + "、" + bt.finery.Show()
}

func NewBigTrouser(finery Finery) BigTrouser {
	return BigTrouser{
		name:   "大裤子",
		finery: finery,
	}
}
