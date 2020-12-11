@echo off
echo running info commands for scidt
scidt -i SimonWilliams>scidtout.txt
scidt -l SimonWilliams>>scidtout.txt
scidt -c SimonWilliams>>scidtout.txt
scidt -n SimonWilliams>>scidtout.txt
echo running info commands for scinct
scinct -i SimonWilliams>scinctout.txt
scinct -l SimonWilliams>>scinctout.txt
scinct -c SimonWilliams>>scinctout.txt
scinct -n SimonWilliams>>scinctout.txt
echo Now compare scinctout.txt to scidtout.txt
pause