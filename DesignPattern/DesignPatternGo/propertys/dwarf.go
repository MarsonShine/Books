package propertys

import "fmt"

type Dwarf struct {
	Id       FighterType
	AxePoint int
	Life     int
	Mana     int
}

func NewDwarf(arrowHit, life, mana int) Dwarf {
	return Dwarf{DwarfType, arrowHit, life, mana}
}

func (d Dwarf) GetId() FighterType {
	return d.Id
}

func (d Dwarf) Fight() {
	fmt.Println("Dwarf Axe Hit-Point:", d.AxePoint)
}

func (d Dwarf) Clone() Fighter {
	return NewDwarf(d.AxePoint, d.Life, d.Mana)
}
