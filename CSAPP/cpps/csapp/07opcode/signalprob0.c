#include <stdio.h>
#include <stdlib.h>
//#include <unistd.h>
//#include <wait.h>
#include <signal.h>

volatile long counter = 2;

void handler1(int sig) {
	//sigset_t mask, prev_mask;
}