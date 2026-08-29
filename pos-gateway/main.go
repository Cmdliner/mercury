package main

import (
	"fmt"
	"time"
)

func main() {
	ch := make(chan string)
	//mssg :=

	go sendMessages(ch)

	time.Sleep(3 * time.Second)

	fmt.Println(<-ch)
}

func sendMessages(msgChan chan string) {
	msgChan <- "Mercury: The POS Gateway"
}
