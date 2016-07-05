# sudoku
This project was started as a way to practice MVC concepts using a Sudoku game and solver.

<hr>

<h4>Current features:</h4>
<ul>
	<li>Sudoku object that can be used to play a game by numerous implementations</li>
	<li>Various solving methods.</li>
	<li>Console app implementation</li>
</ul>

<h4>Long-term vision</h4>
<ul>
	<li>MVC-based web-app implementation</li>
	<li>More solving algorithms implemented (full list in Sudoku.Core/Constants/SolveMethod enum)</li>
	<li>Tool to create new Sudoku boards</li>
	<li>Trainer in web-app that helps user learn and practice solving methods</li>
    <li>If a move is available in a cell, highlight according to the hardest move necessary to solve it in the easiest way.</li>
    <li>Change solving process to find all potential moves before doing any, unless specifically solving whole puzzle</li>
	<li>Better uniqueness tests</li>
	<li>Method to recognize cheating</li>
	<li>Create self-updating exemplar lists of the 100 puzzles which utilize a technique the most often (for each technique)</li>
</ul>

<h4>Near-term to-do:</h4>
<ul>
	<li>Add Bowman's Bingo or other brute force solving technique so that all can be solved</li>
	<li>Add logger which can display solution process or error messages</li>
	<li>Create scoring mechanism</li>
	<li>	Find all potential moves</li>
	<li>	Add the value of the easiest move times a depreciating factor</li>
	<li>	Depreciating factor is dependent on the number and difficulty of other available moves</li>
	<li>	i.e. if 9 naked singles & 6 hidden singles etc: 10 * (1 - .1*9 - .05*6 etc)</li>
	<li>	solve move and repeat</li>
</ul>

<h4>Sudoku-based incremental idea</h4>
<ul>
    <li>Earn exp as certain moves are performed, which eventually allow to buy auto-solving agents</li>
    <li>Earn money dependent on difficulty of move and time</li>
    <li>Moves which can't reasonably be solved but are filled in anyway earn no income</li>
    <li>Solving agents earn a portion/all? of money, so self-solving is incentivized</li>
    <li>Solving agents are expensive and slow, but can add up / level to make solving much faster</li>
    <li>Similar to "a matter of scale", solving lots of easy ones gives access to more difficult puzzles</li>
    <li>Easy puzzles may have a "threshhold" for reasonable techniques, which gets higher for harder puzzles</li>
    <li>Consumable items? Hints? Highlighters? Very expensive permanent unlocks?</li>
    <li>4x4 and 6x6 variants? 12x12 and 16x16? Other types?</li>
</ul>
