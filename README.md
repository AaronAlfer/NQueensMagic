# N Queens Magic
*A program that solves the N Queens puzzle*
## Introduction
So I was figuring out how to program my own chess engine, and suddenly I stumbled upon an article saying that some Math institute offers $1M to anyone who can 'solve a chess puzzle'. It had something to do with the P vs NP problem, and the thing was a bit trickier than what the title suggested (who would guess).

[An article on the subject (not the one I saw first)](https://www.st-andrews.ac.uk/news/archive/2017/title,1539813,en.php)  
[P vs NP](http://claymath.org/millennium-problems/p-vs-np-problem)

Of course, I didn't expect to come up with a solution to the problem. But it made me curious: I thought, maybe I could experiment and arrive at some interesting conclusions. 'Thanks' to the media, I misenterpreted the problem thinking that one needs to invent an algorithm that finds ALL solutions to the puzzle (starting from an empty board) really fast. For example, the standard 8 Queens puzzle has 92 unique solutions. So, to my understanding, the algorithm was supposed to find all of them, and then store them somehow. That wasn't quite the case, and I'll explain in a minute.

At first, I tried to come up with a non-conventional way of placing 8 queens so that the combination ends up being one of the 12 [fundamental solutions](https://en.wikipedia.org/wiki/Eight_queens_puzzle#Solutions). Interestingly enough, I found that if you take one solution, and shift all the queens one square in any direction (the utmost queen jumps over and ends up on the other side), you can get another fundamental solution! Just by doing that, I managed to find 8 solutions out of 12. So if you're learning chess and your teacher ever asks you to solve this puzzle in various ways, you can use this method. Anyway, as you can imagine, there is still a couple of problems. First of all, not all solutions can be found this way. Secondly, we still need to find the first solution somehow. And lastly, it all becomes much more complicated on larger boards: the algorithm has to change depending on N. So I gave up on that idea.

Eventually I decided: why not use the good old [backtracking](https://en.wikipedia.org/wiki/Backtracking) method but somehow make it more efficient than... well, anything that implements a 2D array of some sort? I just wanted to get some meaningful and stable results. Again, I wasn't trying to solve a millennium problem: I was practicing, and that was it. So I sat in front of the chessboard and thought: 'If only I could use [bitboards](https://chessprogramming.wikispaces.com/Bitboards) here, but at the same time be able to scale the board'. And then an idea occured to me which seemed brilliant: why the hell not make an array of bitboards so that each bitboard acts like an individual element in a matrix? Then I started coding, and what you see here implements this very idea.

It was only after I finished the 1st version of the project that I realised: the original problem was not about finding all of the solutions per N. Instead, it stated: you either prove that there is an algorithm that can solve the completion(!) puzzle in a polynomial time OR prove that there isn't one. The 'completion' part here is key. What this means is basically another version of the puzzle in which you need to complete a given set of queens, i.e. having some of the queens already placed on the board, place the rest without moving the original ones. Now it seemed more like Sudoku. My algorithm was exponential, not polynomial (alas), but I modified the program so that it was now able to perform both tasks: either find all solutions, or complete a randomly generated or specified preset. I found the results to be quite interesting, and they are described in detail in the corresponding section of this document.

## Project Structure
Text
## How It Works
Text
## Results
Text
## Conclusion
Text
