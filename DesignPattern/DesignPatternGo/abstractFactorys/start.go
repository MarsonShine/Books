package abstractfactorys

import "fmt"

func Start() {
	adidasFactory, _ := getSportsFactory("adidas")
	adidasShoe := adidasFactory.makeShoe()
	adidasShirt := adidasFactory.makeShirt()

	fmt.Printf("Logo: %s", adidasShoe.getLogo())
	fmt.Println()
	fmt.Printf("Size: %d", adidasShoe.getSize())
	fmt.Println()

	fmt.Printf("Logo: %s", adidasShirt.getLogo())
	fmt.Println()
	fmt.Printf("Size: %d", adidasShirt.getSize())
	fmt.Println()
}
