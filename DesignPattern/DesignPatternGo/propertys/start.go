package propertys

var dataHistoryList map[int]Fighter

func Start() {
	localCache()
	fighter := dataHistoryList[int(SolidierType)]
	f, ok := fighter.(Solidier)
	if ok {
		newFighter := f.Clone().(Solidier)
		newFighter.GunHit = 45
		newFighter.Fight()
	}

	elf := dataHistoryList[int(ElfType)]
	e, ok := elf.(Elf)
	if ok {
		newElf := e.Clone().(Elf)
		newElf.Fight()
	} else {
		elf := NewElf(15, 30, 3)
		dataHistoryList[int(elf.GetId())] = elf
		elf.Fight()
	}
	elf2 := dataHistoryList[int(ElfType)]
	e2, ok := elf2.(Elf)
	if ok {
		newElf2 := e2.Clone().(Elf)
		newElf2.ArrowHit = 35
		newElf2.Fight()
	}

	dwarf := dataHistoryList[int(DwarfType)]
	d, ok := dwarf.(Dwarf)
	if ok {
		newDwarf := d.Clone()
		newDwarf.Fight()
	} else {
		dwarf := NewDwarf(50, 40, 15)
		dataHistoryList[int(DwarfType)] = dwarf
		dwarf.Fight()
	}
}

func localCache() {
	solider := NewSoldier(30, 20, 5)
	dwarf := NewDwarf(50, 40, 15)
	dataHistoryList = make(map[int]Fighter)
	dataHistoryList[int(dwarf.GetId())] = dwarf
	dataHistoryList[int(solider.GetId())] = solider
}
