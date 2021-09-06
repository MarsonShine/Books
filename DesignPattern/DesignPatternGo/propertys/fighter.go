package propertys

type FighterType int

const (
	SolidierType FighterType = 1
	ElfType                  = 2
	DwarfType                = 3
)

type Fighter interface {
	GetId() FighterType
	Fight()
	Clone() Fighter
}
