#pragma once

#include "common.h"
#include "index.h"
#include "game.h"
#include "tree.h"
#include "gfile.h"
#include "pbook.h"
#include "pgnparse.h"
#include "timer.h"
#include "stored.h"

// Filter operations:

typedef uint filterOpT;
const filterOpT FILTEROP_AND = 0;
const filterOpT FILTEROP_OR = 1;
const filterOpT FILTEROP_RESET = 2;

// TreeCache size for each open database:
const uint SCID_TreeCacheSize = 1000; //250

// Secondary (memory only) TreeCache size:
const uint SCID_BackupCacheSize = 100;

// Number of undo levels
#define UNDO_MAX 20

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Scid database stats structure:
//    This is maintained and recalculated each time a game in the
//    database is modified to save time updating the file stats window.
//

struct ecoStatsT {
	uint  count;
	uint  results[NUM_RESULT_TYPES];
};

struct scidStatsT {
	uint  flagCount[IDX_NUM_FLAGS];  // Num of games with each flag set.
	dateT minDate;
	dateT maxDate;
	unsigned long long  nYears;
	unsigned long long  sumYears;
	uint  nResults[NUM_RESULT_TYPES];
	uint  nRatings;
	unsigned long long  sumRatings;
	uint  minRating;
	uint  maxRating;
	ecoStatsT ecoCount0[1];
	ecoStatsT ecoCount1[5];
	ecoStatsT ecoCount2[50];
	ecoStatsT ecoCount3[500];
	ecoStatsT ecoCount4[500 * 26];
};


//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Scid database structure:
//
// Might be usefull to have a "last sorted" field
//   wtf isn't gameNumber uint ? Changing it thus cause coredumps &&&
struct scidBaseT {
	Index* idx;           // the Index file in memory for this base.
	NameBase* nb;            // the NameBase file in memory.
	Game* game;          // the active game for this base.
	Game* undoGame[UNDO_MAX]; // array of games kept for undos
	int          undoIndex;
	int          undoMax;
	int          undoCurrent;   // which undo buffer has the currently saved game
	bool         undoCurrentNotAvail;	// if buffer gets filled, we cant unset gameAltered
	int          gameNumber;    // game number of active game.
	bool         gameAltered;   // true if game is modified
	bool         inUse;         // true if the database is open (in use).
	uint         numGames;
	bool         memoryOnly;

	scidStatsT   stats;         // Counts of flags, average rating, etc.

	treeT        tree;
	TreeCache* treeCache;
	TreeCache* backupCache;
	uint         treeSearchTime;

	fileNameT    fileName;      // File name without ".si" suffix
	fileNameT    realFileName;  // File name including ".si" suffix
	fileModeT    fileMode;      // Read-only, write-only, or both.
	GFile* gfile;
	ByteBuffer* bbuf;
	TextBuffer* tbuf;
	Filter* filter;
	Filter* dbFilter;
	Filter* treeFilter;
	uint* duplicates;  // For each game: idx of duplicate game + 1,
							  // or 0 if there is no duplicate.
};

using System::String;
using namespace System::Runtime::InteropServices;
namespace ScincFuncs {
	///<summary>
	/// Class that hold functions relating to the SCID database 
	///</summary>
	public ref class Base
	{
	public:
		static Base();
		static int Autoloadgame(bool getbase, unsigned int basenum);
		static int Open(String^ basenm);
		static int Close();
		static bool Isreadonly();
		static int NumGames();
		static int Getfilename([Out] String^% name);
		static bool InUse();
		static int Create(String^ basenm);
		static int Import(int% numgames, String^% msgs, String^ pgnfile);
		static int CountFree();
		static int Current();

	};
	/// <summary>
	/// Class that hold functions relating to the SCID clipbase
	/// </summary>
	public ref class Clipbase
	{
	public:
		static int Clear();

	};
	/// <summary>
	/// Class that hold functions relating to games in a SCID clipbase
	/// </summary>
	public ref class ScidGame
	{
	public:
		static int Load(unsigned int gnum);
		static int Save(unsigned int gnum);
		static int StripComments();
		static int GetTag(String^ tag, String^% val);
		static int SetTag(String^ tag, String^ val);
		static int List(String^% glist, unsigned int start, unsigned int count, String^ formatStr);
		static int ScincFuncs::ScidGame::Pgn(String^% pgn);

	};
	/// <summary>
	/// Class that hold functions relating to the ECO book
	/// </summary>
	public ref class Eco
	{
	public:
		static int Read(String^ econm);
		static int Base(String^% msgs);

	};
	/// <summary>
	/// Class that hold functions relating to databse filter
	/// </summary>
	public ref class Filt
	{
	public:
		static int Count();
	};
	/// <summary>
	/// Class that hold functions relating to the stats tree
	/// </summary>
	public ref class Tree
	{
	public:
		static int Search(String^ fenstr, String^% treestr);
	};
	/// <summary>
	/// Class that hold functions relating to search
	/// </summary>
	public ref class Search
	{
	public:
		static int Board(String^ fenstr);
	};

}
