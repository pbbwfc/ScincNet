# Usage:  tcscid importWhite.tcl

# Open the database:
set basename D:/tmp/WhiteTCL
sc_base create $basename

if {[catch {sc_base open $basename} result]} {
    puts stderr "Error opening database \"$basename\": $result"
    exit 1
}
if {[sc_base isReadOnly]} {
    puts stderr "Error: database \"$basename\" is read-only."
    exit 1
}
set num [sc_base numGames] 

puts "number of games: $num"

set fol "D:/tmp/"
set pgns [list "Benko.pgn" "Benoni.pgn" "Budapest.pgn" "Dutch.pgn" "Grunfeld.pgn" "KingsIndian.pgn" "OldIndian.pgn" "QGA.pgn" "QGDmain.pgn" "QGDtarr.pgn" "QGDtri.pgn" "QGDunus.pgn" "Slav.pgn"]
foreach pgn $pgns  {
    set pgnfile [file join $fol $pgn]

    if {[catch {sc_base import file $pgnfile} result]} {
        puts stderr "Error importing \"$pgnfile\": $result"
        exit 1
    }
    set numImported [lindex $result 0]
    set warnings [lindex $result 1]
    puts "Imported $numImported games from $pgnfile"
    if {$warnings == ""} {
        puts "There were no PGN errors or warnings."
    } else {
        puts "PGN errors/warnings:"
        puts $warnings
    }
}

sc_base close

# now classify
# Open the ECO file:
set ecofile "D:/tmp/scid.eco"
if {[catch {sc_eco read $ecofile} result]} {
    puts stderr "Error reading ECO file: $result"
    exit 1
}

# Open the database:
if {[catch {sc_base open $basename} result]} {
    puts stderr "Error opening database \"$basename\": $result"
    exit 1
}
if {[sc_base isReadOnly]} {
    puts stderr "Error: database \"$basename\" is read-only."
    exit 1
}

puts "Classifying games..."
puts [sc_eco base 1 1]
sc_base close

#now strip comments
# Open the database:
if {[catch {sc_base open $basename} result]} {
    puts stderr "Error opening database \"$basename\": $result"
    exit 1
}
if {[sc_base isReadOnly]} {
    puts stderr "Error: database \"$basename\" is read-only."
    exit 1
}

for {set i 1} {$i <= [sc_base numGames]} {incr i} {
    if {[catch { sc_game load $i }]} {
        puts "Error: could not load game number $i"
        exit 1
    }
    sc_game strip comments

    sc_game save $i
}
sc_base close

#now add names

set nms [list "Benko" "Benoni" "Budapest" "Dutch" "Grunfeld" "Kings Indian" "Old Indian" "QGA" "QGD main" "QGD tarrasch" "QGD triangle" "QGD unusual" "Slav"]

# Open the database:
if {[catch {sc_base open $basename} result]} {
    puts stderr "Error opening database \"$basename\": $result"
    exit 1
}
if {[sc_base isReadOnly]} {
    puts stderr "Error: database \"$basename\" is read-only."
    exit 1
}

for {set i 1} {$i <= [sc_base numGames]} {incr i} {
    if {[catch { sc_game load $i }]} {
        puts "Error: could not load game number $i"
        exit 1
    }
    set nm [lindex $nms [expr $i-1]]
    sc_game tags set -white $nm

    sc_game save $i
}
sc_base close
