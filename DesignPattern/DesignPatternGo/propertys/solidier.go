package propertys

import "fmt"

type Solidier struct {
	Id     FighterType
	GunHit int
	Life   int
	Mana   int
}

func NewSoldier(gunHit, life, mana int) Solidier {
	solidier := Solidier{
		SolidierType, gunHit, life, mana}
	return solidier
}

func (s Solidier) GetId() FighterType {
	return s.Id
}

func (s Solidier) Fight() {
	fmt.Println("Soldier Gun Hit-Points:", s.GunHit)
}

func (s Solidier) Clone() Fighter {
	return NewSoldier(
		s.GunHit, s.Life, s.Mana)
}
