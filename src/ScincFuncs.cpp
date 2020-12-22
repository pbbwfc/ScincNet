
#include "ScincFuncs.h"
#include <msclr/marshal.h>
#include <set>

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Global variables:

static scidBaseT* db = NULL;       // current database.
static scidBaseT* clipbase = NULL; // clipbase database.
static scidBaseT* dbList = NULL;   // array of database slots.
static int currentBase = 0;
static Position* scratchPos = NULL;                         // temporary "scratch" position.
static Game* scratchGame = NULL;                            // "scratch" game for searches, etc.
static PBook* ecoBook = NULL;                               // eco classification pbook.


// Default maximum number of games in the clipbase database:
const uint CLIPBASE_MAX_GAMES = 5000000; // 5 million
// Actual current maximum number of games in the clipbase database:
static uint clipbaseMaxGames = CLIPBASE_MAX_GAMES;
// MAX_BASES is the maximum number of databases that can be open,
// including the clipbase database.
const int MAX_BASES = 9;
const int CLIPBASE_NUM = MAX_BASES - 1;

static char decimalPointChar = '.';


//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// recalcFlagCounts:
//    Updates all precomputed stats about the database: flag counts,
//    average rating, date range, etc.
void recalcFlagCounts(scidBaseT* basePtr)
{
	scidStatsT* stats = &(basePtr->stats);
	uint i;

	// Zero out all stats:
	for (i = 0; i < IDX_NUM_FLAGS; i++)
	{
		stats->flagCount[i] = 0;
	}
	stats->nRatings = 0;
	stats->sumRatings = 0;
	stats->minRating = 0;
	stats->maxRating = 0;
	stats->minDate = ZERO_DATE;
	stats->maxDate = ZERO_DATE;
	stats->nYears = 0;
	stats->sumYears = 0;
	for (i = 0; i < NUM_RESULT_TYPES; i++)
	{
		stats->nResults[i] = 0;
	}
	for (i = 0; i < 1; i++)
	{
		stats->ecoCount0[i].count = 0;
		stats->ecoCount0[i].results[RESULT_White] = 0;
		stats->ecoCount0[i].results[RESULT_Black] = 0;
		stats->ecoCount0[i].results[RESULT_Draw] = 0;
		stats->ecoCount0[i].results[RESULT_None] = 0;
	}
	for (i = 0; i < 5; i++)
	{
		stats->ecoCount1[i].count = 0;
		stats->ecoCount1[i].results[RESULT_White] = 0;
		stats->ecoCount1[i].results[RESULT_Black] = 0;
		stats->ecoCount1[i].results[RESULT_Draw] = 0;
		stats->ecoCount1[i].results[RESULT_None] = 0;
	}
	for (i = 0; i < 50; i++)
	{
		stats->ecoCount2[i].count = 0;
		stats->ecoCount2[i].results[RESULT_White] = 0;
		stats->ecoCount2[i].results[RESULT_Black] = 0;
		stats->ecoCount2[i].results[RESULT_Draw] = 0;
		stats->ecoCount2[i].results[RESULT_None] = 0;
	}
	for (i = 0; i < 500; i++)
	{
		stats->ecoCount3[i].count = 0;
		stats->ecoCount3[i].results[RESULT_White] = 0;
		stats->ecoCount3[i].results[RESULT_Black] = 0;
		stats->ecoCount3[i].results[RESULT_Draw] = 0;
		stats->ecoCount3[i].results[RESULT_None] = 0;
	}
	for (i = 0; i < 500 * 26; i++)
	{
		stats->ecoCount4[i].count = 0;
		stats->ecoCount4[i].results[RESULT_White] = 0;
		stats->ecoCount4[i].results[RESULT_Black] = 0;
		stats->ecoCount4[i].results[RESULT_Draw] = 0;
		stats->ecoCount4[i].results[RESULT_None] = 0;
	}
	// Read stats from index entry of each game:
	for (uint gnum = 0; gnum < basePtr->numGames; gnum++)
	{
		IndexEntry* ie = basePtr->idx->FetchEntry(gnum);
		stats->nResults[ie->GetResult()]++;
		eloT elo = ie->GetWhiteElo();
		if (elo > 0)
		{
			stats->nRatings++;
			stats->sumRatings += elo;
			if (stats->minRating == 0)
			{
				stats->minRating = elo;
			}
			if (elo < stats->minRating)
			{
				stats->minRating = elo;
			}
			if (elo > stats->maxRating)
			{
				stats->maxRating = elo;
			}
			basePtr->nb->AddElo(ie->GetWhite(), elo);
		}
		elo = ie->GetBlackElo();
		if (elo > 0)
		{
			stats->nRatings++;
			stats->sumRatings += elo;
			if (stats->minRating == 0)
			{
				stats->minRating = elo;
			}
			if (elo < stats->minRating)
			{
				stats->minRating = elo;
			}
			if (elo > stats->maxRating)
			{
				stats->maxRating = elo;
			}
			basePtr->nb->AddElo(ie->GetBlack(), elo);
		}
		dateT date = ie->GetDate();
		if (gnum == 0)
		{
			stats->maxDate = stats->minDate = date;
		}
		if (date_GetYear(date) > 0)
		{
			if (date < stats->minDate)
			{
				stats->minDate = date;
			}
			if (date > stats->maxDate)
			{
				stats->maxDate = date;
			}
			stats->nYears++;
			stats->sumYears += date_GetYear(date);
			basePtr->nb->AddDate(ie->GetWhite(), date);
			basePtr->nb->AddDate(ie->GetBlack(), date);
		}

		for (uint flag = 0; flag < IDX_NUM_FLAGS; flag++)
		{
			bool value = ie->GetFlag(1 << flag);
			if (value)
			{
				stats->flagCount[flag]++;
			}
		}

		ecoT eco = ie->GetEcoCode();
		ecoStringT ecoStr;
		eco_ToExtendedString(eco, ecoStr);
		uint length = strLength(ecoStr);
		resultT result = ie->GetResult();
		if (length >= 3)
		{
			uint code = 0;
			stats->ecoCount0[code].count++;
			stats->ecoCount0[code].results[result]++;
			code = ecoStr[0] - 'A';
			stats->ecoCount1[code].count++;
			stats->ecoCount1[code].results[result]++;
			code = (code * 10) + (ecoStr[1] - '0');
			stats->ecoCount2[code].count++;
			stats->ecoCount2[code].results[result]++;
			code = (code * 10) + (ecoStr[2] - '0');
			stats->ecoCount3[code].count++;
			stats->ecoCount3[code].results[result]++;
			if (length >= 4)
			{
				code = (code * 26) + (ecoStr[3] - 'a');
				stats->ecoCount4[code].count++;
				stats->ecoCount4[code].results[result]++;
			}
		}
	}
}

static ScincFuncs::Base::Base()
{
	// Initialise global Scid database variables:
	dbList = new scidBaseT[MAX_BASES];

	for (int base = 0; base < MAX_BASES; base++)
	{
		db = &(dbList[base]);
		db->idx = new Index;
		db->nb = new NameBase;
		db->game = new Game;
		for (int u = 0; u < UNDO_MAX; u++)
			db->undoGame[u] = NULL;
		db->undoMax = -1;
		db->undoIndex = -1;
		db->undoCurrent = -1;
		db->undoCurrentNotAvail = false;
		db->gameNumber = -1;
		db->gameAltered = false;
		db->gfile = new GFile;
		// TODO: Bases should be able to share common buffers!!!
		db->bbuf = new ByteBuffer;
		db->bbuf->SetBufferSize(BBUF_SIZE);
		db->tbuf = new TextBuffer;
		db->tbuf->SetBufferSize(TBUF_SIZE);
		strCopy(db->fileName, "");
		strCopy(db->realFileName, "");
		db->fileMode = FMODE_Both;
		db->inUse = false;
		db->filter = new Filter(0);
		db->dbFilter = db->filter;
		db->treeFilter = NULL;
		db->numGames = 0;
		db->memoryOnly = false;
		db->duplicates = NULL;
		db->idx->SetDescription("NOT OPEN");

		recalcFlagCounts(db);

		db->tree.moveCount = db->tree.totalCount = 0;
		db->treeCache = NULL;

		db->treeSearchTime = 0;
	}
	// Initialise the clipbase database:
	clipbase = &(dbList[CLIPBASE_NUM]);
	clipbase->gfile->CreateMemoryOnly();
	clipbase->idx->CreateMemoryOnly();
	clipbase->idx->SetType(2);
	clipbase->idx->SetDescription("Temporary database, not kept on disk.");
	clipbase->inUse = true;
	clipbase->memoryOnly = true;

	clipbase->treeCache = new TreeCache;
	clipbase->treeCache->SetCacheSize(SCID_TreeCacheSize);
	clipbase->backupCache = new TreeCache;
	clipbase->backupCache->SetCacheSize(SCID_BackupCacheSize);
	clipbase->backupCache->SetPolicy(TREECACHE_Oldest);

	currentBase = 0;
	scratchPos = new Position;
	scratchGame = new Game;
	db = &(dbList[currentBase]);
}

//  MISC functions

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// findEmptyBase:
//    returns a number from 0 to MAX_BASES - 1 if an empty
//    database slot exists; or returns -1 if a maximum number of bases
//    are already in use.
int findEmptyBase(void)
{
	for (int i = 0; i < MAX_BASES; i++)
	{
		if (!dbList[i].inUse)
		{
			return i;
		}
	}
	return -1;
}

void clearFilter(scidBaseT* dbase, uint size)
{
	if (dbase->dbFilter && dbase->dbFilter != dbase->filter)
		delete dbase->dbFilter;
	if (dbase->filter)
		delete dbase->filter;
	if (dbase->treeFilter)
		delete dbase->treeFilter;
	dbase->filter = new Filter(size);
	dbase->dbFilter = dbase->filter;
	dbase->treeFilter = NULL;
}

//  DATABASE functions

// base_opened:
//  Returns a slot number if the named database is already
//  opened in Scid, or -1 if it is not open.
int base_opened(const char* filename)
{
	for (int i = 0; i < CLIPBASE_NUM; i++)
	{
		if (dbList[i].inUse && strEqual(dbList[i].realFileName, filename))
		{
			return i;
		}
	}

	// OK, An exact same file name was not found, but we may have compared
	// absolute path (e.g. from a File Open dialog) with a relative one
	// (e.g. from a command-line argument).
	// To check further, return true if two names have the same tail
	// (part after the last "/"), device and inode number:

	const char* tail = strLastChar(filename, '/');
	if (tail == NULL)
	{
		tail = filename;
	}
	else
	{
		tail++;
	}
	for (int j = 0; j < CLIPBASE_NUM; j++)
	{
		if (!dbList[j].inUse)
		{
			continue;
		}
		const char* ftail = strLastChar(dbList[j].realFileName, '/');
		if (ftail == NULL)
		{
			ftail = dbList[j].realFileName;
		}
		else
		{
			ftail++;
		}

		if (strEqual(ftail, tail))
		{
			struct stat s1;
			struct stat s2;
			if (stat(ftail, &s1) != 0)
			{
				continue;
			}
			if (stat(tail, &s2) != 0)
			{
				continue;
			}
			if (s1.st_dev == s2.st_dev && s1.st_ino == s2.st_ino)
			{
				return j;
			}
		}
	}
	return -1;
}

/// <summary>
/// Autoloadgame: Sets or returns the autoload number for the database - the game number to load.
/// </summary>
/// <param name="getbase">Whether to get or set the database</param>
/// <param name="basenum">The number to set</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Base::Autoloadgame(bool getbase, unsigned int basenum)
{
	if (getbase)
	{
		return db->idx->GetAutoLoad();
	}

	if (!db->inUse)
	{
		return -1;
	}
	if (db->fileMode == FMODE_ReadOnly)
	{
		return -2;
	}

	db->idx->SetAutoLoad(basenum);
	db->idx->WriteHeader();
	return 0;
}

/// <summary>
/// Open: takes a database name and opens the database.
///    If either the index file or game file cannot be opened for
///    reading and writing, then the database is opened read-only
///    and will not be alterable.
/// </summary>
/// <param name="basenm">The name of the database</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Base::Open(String^ basenm)
{
	msclr::interop::marshal_context oMarshalContext;

	const char* basename = oMarshalContext.marshal_as<const char*>(basenm);

	// Check that this base is not already opened:
	fileNameT realFileName;
	strCopy(realFileName, basename);
	strAppend(realFileName, INDEX_SUFFIX);
	if (base_opened(realFileName) >= 0)
	{
		return -1;
	}

	// Find an empty database slot to use:
	int oldBaseNum = currentBase;
	if (db->inUse)
	{
		int newBaseNum = findEmptyBase();
		if (newBaseNum == -1)
		{
			return -1;
		}
		currentBase = newBaseNum;
		db = &(dbList[currentBase]);
	}

	db->idx->SetFileName(basename);
	db->nb->SetFileName(basename);

	db->memoryOnly = false;
	db->fileMode = FMODE_Both;

	errorT err;
	err = db->idx->OpenIndexFile(db->fileMode);
	err = db->nb->ReadNameFile();
	err = db->gfile->Open(basename, db->fileMode);



	db->idx->ReadEntireFile();
	db->numGames = db->idx->GetNumGames();
	// Initialise the filter: all games match at move 1 by default.
	clearFilter(db, db->numGames);

	strCopy(db->fileName, basename);
	strCopy(db->realFileName, realFileName);
	db->inUse = true;
	db->gameNumber = -1;

	if (db->treeCache == NULL)
	{
		db->treeCache = new TreeCache;
		db->treeCache->SetCacheSize(SCID_TreeCacheSize);
		db->backupCache = new TreeCache;
		db->backupCache->SetCacheSize(SCID_BackupCacheSize);
		db->backupCache->SetPolicy(TREECACHE_Oldest);
	}

	db->treeCache->Clear();
	db->backupCache->Clear();

	return (currentBase + 1);
}

/// <summary>
/// Close: Closes the current or specified database.
/// </summary>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Base::Close()
{
	scidBaseT* basePtr = db;

	if (!basePtr->inUse)
	{
		return -1;
	}

	// reset undo data
	basePtr->undoMax = -1;
	basePtr->undoIndex = -1;
	basePtr->undoCurrent = -1;
	basePtr->undoCurrentNotAvail = false;
	for (int u = 0; u < UNDO_MAX; u++)
	{
		if (basePtr->undoGame[u] != NULL)
		{
			delete basePtr->undoGame[u];
			basePtr->undoGame[u] = NULL;
		}
	}

	// If the database is the clipbase, do not close it, just clear it:
	if (basePtr == clipbase)
	{
		return Clipbase::Clear();
	}
	basePtr->idx->CloseIndexFile();
	basePtr->idx->Clear();
	basePtr->nb->Clear();
	basePtr->gfile->Close();
	basePtr->idx->SetDescription("NOT OPEN");

	clearFilter(basePtr, 0);

	if (basePtr->duplicates != NULL)
	{
		delete[] basePtr->duplicates;
		basePtr->duplicates = NULL;
	}

	basePtr->inUse = false;
	basePtr->gameNumber = -1;
	basePtr->numGames = 0;
	recalcFlagCounts(basePtr);
	strCopy(basePtr->fileName, "<empty>");
	basePtr->treeCache->Clear();
	basePtr->backupCache->Clear();
	return 0;
}

/// <summary>
/// Isreadoonly: is the base read only
/// </summary>
/// <returns>returns true if read only</returns>
bool ScincFuncs::Base::Isreadonly()
{
	return db->inUse && db->fileMode == FMODE_ReadOnly;
}

/// <summary>
/// NumGames: Takes optional database number and returns number of games.
/// </summary>
/// <returns>returns number of games</returns>
int ScincFuncs::Base::NumGames()
{
	scidBaseT* basePtr = db;
	return basePtr->inUse ? basePtr->numGames : 0;
}

/// <summary>
/// Getfilename: get the name of the current database file.
///   Returns "[empty]" for an empty base, "[clipbase]" for the clipbase.
/// </summary>
/// <param name="name">The name variable passed by reference</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Base::Getfilename(String^% name)
{
	scidBaseT* basePtr = db;
	if (!basePtr->inUse)
	{
		name = gcnew System::String("[empty]");
	}
	else if (basePtr == clipbase)
	{
		name = gcnew System::String("[clipbase]");
	}
	else
	{
		name = gcnew System::String(basePtr->fileName);
	}

	return 0;
}

/// <summary>
/// InUse: Returns 1 if the database slot is in use; 0 otherwise.
/// </summary>
/// <returns>returns 1 if the database slot is in use; 0 otherwise.</returns>
bool ScincFuncs::Base::InUse()
{
	scidBaseT* basePtr = db;

	return basePtr->inUse;
}

int createbase(const char* filename, scidBaseT* base, bool memoryOnly)
{
	if (base->inUse)
	{
		return -1;
	}

	base->idx->SetFileName(filename);
	base->idx->SetDescription("");
	base->nb->Clear();
	base->nb->SetFileName(filename);
	base->fileMode = FMODE_Both;
	base->memoryOnly = false;

	if (memoryOnly)
	{
		base->memoryOnly = true;
		base->gfile->CreateMemoryOnly();
		base->idx->CreateMemoryOnly();
		base->idx->SetDescription("READ_ONLY");
		base->fileMode = FMODE_ReadOnly;
	}
	else
	{
		if (base->idx->CreateIndexFile(FMODE_Both) != OK)
		{
			return -2;
		}

		base->idx->WriteHeader();
		if (base->nb->WriteNameFile() != OK)
		{
			return -3;
		}
		base->idx->ReadEntireFile();

		if (base->gfile->Create(filename, FMODE_Both) != OK)
		{
			return -4;
		}
	}

	// Initialise the filter:
	base->numGames = base->idx->GetNumGames();
	clearFilter(base, base->numGames);

	strCopy(base->fileName, filename);
	base->inUse = true;
	base->gameNumber = -1;
	if (base->treeCache == NULL)
	{
		base->treeCache = new TreeCache;
		base->treeCache->SetCacheSize(SCID_TreeCacheSize);
		base->backupCache = new TreeCache;
		base->backupCache->SetCacheSize(SCID_BackupCacheSize);
		base->backupCache->SetPolicy(TREECACHE_Oldest);
	}
	base->treeCache->Clear();
	base->backupCache->Clear();
	recalcFlagCounts(base);

	// Ensure an old treefile is not still around:
	if (!memoryOnly)
	{
		removeFile(base->fileName, TREEFILE_SUFFIX);
	}
	return 0;
}

/// <summary>
/// Create: creates a new database give a string of folder with name
/// </summary>
/// <param name="basenm">The name of the new database</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Base::Create(String^ basenm)
{
	msclr::interop::marshal_context oMarshalContext;

	const char* basename = oMarshalContext.marshal_as<const char*>(basenm);

	// Check that this base is not already opened:
	fileNameT realFileName;
	strCopy(realFileName, basename);
	strAppend(realFileName, INDEX_SUFFIX);
	if (base_opened(realFileName) >= 0)
	{
		return -1;
	}

	// Find another slot if current slot is used:
	int newBaseNum = currentBase;
	if (db->inUse)
	{
		newBaseNum = findEmptyBase();
		if (newBaseNum == -1)
		{
			return -2;
		}
	}

	scidBaseT* baseptr = &(dbList[newBaseNum]);
	if (createbase(basename, baseptr, false) != 0)
	{
		return -3;
	}

	currentBase = newBaseNum;

	db = baseptr;

	db->inUse = true;
	strCopy(db->realFileName, basename);

	return newBaseNum + 1;
}

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// sc_savegame:
//    Called by sc_game_save and by clipbase functions to save
//    a new game or replacement game to a database.
//    
//   (NB - See below for another sc_savegame, which is used in switcher game copy (and else???))
int sc_savegame(Game* game, gameNumberT gnum, scidBaseT* base)
{
	char temp[200];
	if (!base->inUse)
	{
		return -1;
	}
	if (base->fileMode == FMODE_ReadOnly && !(base->memoryOnly))
	{
		return -2;
	}
	if (base == clipbase && base->numGames >= clipbaseMaxGames)
	{
		return -3;
	}

	base->bbuf->Empty();
	bool replaceMode = false;
	gameNumberT gNumber = 0;
	if (gnum > 0)
	{
		gNumber = gnum - 1; // Number games from zero.
		replaceMode = true;
	}

	// Grab a new idx entry, if needed:
	IndexEntry* oldIE = NULL;
	IndexEntry iE;
	iE.Init();

	if (game->Encode(base->bbuf, &iE) != OK)
	{
		return -4;
	}

	// game->Encode computes flags, so we have to re-set flags if replace mode
	if (replaceMode)
	{
		oldIE = base->idx->FetchEntry(gNumber);
		// Remember previous user-settable flags:
		for (uint flag = 0; flag < IDX_NUM_FLAGS; flag++)
		{
			char flags[32];
			oldIE->GetFlagStr(flags, NULL);
			iE.SetFlagStr(flags);
		}
	}
	else
	{
		// add game without resetting the index, because it has been filled by game->encode above
		if (base->idx->AddGame(&gNumber, &iE, false) != OK)
		{
			sprintf(temp, "Scid maximum games (%u) reached.\n", MAX_GAMES);
			return -5;
		}
		base->numGames = base->idx->GetNumGames();
	}

	base->bbuf->BackToStart();

	// Now try writing the game to the gfile:
	uint offset = 0;
	if (base->gfile->AddGame(base->bbuf, &offset) != OK)
	{
		return -6;
	}
	iE.SetOffset(offset);
	iE.SetLength(base->bbuf->GetByteCount());

	// Now we add the names to the NameBase
	// If replacing, we decrement the frequency of the old names.
	const char* s;
	idNumberT id = 0;

	// WHITE:
	s = game->GetWhiteStr();
	if (!s)
	{
		s = "?";
	}
	if (base->nb->AddName(NAME_PLAYER, s, &id) == ERROR_NameBaseFull)
	{
		return -8;
	}
	base->nb->IncFrequency(NAME_PLAYER, id, 1);
	iE.SetWhite(id);

	// BLACK:
	s = game->GetBlackStr();
	if (!s)
	{
		s = "?";
	}
	if (base->nb->AddName(NAME_PLAYER, s, &id) == ERROR_NameBaseFull)
	{
		return -9;
	}
	base->nb->IncFrequency(NAME_PLAYER, id, 1);
	iE.SetBlack(id);

	// EVENT:
	s = game->GetEventStr();
	if (!s)
	{
		s = "?";
	}
	if (base->nb->AddName(NAME_EVENT, s, &id) == ERROR_NameBaseFull)
	{
		return -10;
	}
	base->nb->IncFrequency(NAME_EVENT, id, 1);
	iE.SetEvent(id);

	// SITE:
	s = game->GetSiteStr();
	if (!s)
	{
		s = "?";
	}
	if (base->nb->AddName(NAME_SITE, s, &id) == ERROR_NameBaseFull)
	{
		return -11;
	}
	base->nb->IncFrequency(NAME_SITE, id, 1);
	iE.SetSite(id);

	// ROUND:
	s = game->GetRoundStr();
	if (!s)
	{
		s = "?";
	}
	if (base->nb->AddName(NAME_ROUND, s, &id) == ERROR_NameBaseFull)
	{
		return -12;
	}
	base->nb->IncFrequency(NAME_ROUND, id, 1);
	iE.SetRound(id);

	// If replacing, decrement the frequency of the old names:
	if (replaceMode)
	{
		base->nb->IncFrequency(NAME_PLAYER, oldIE->GetWhite(), -1);
		base->nb->IncFrequency(NAME_PLAYER, oldIE->GetBlack(), -1);
		base->nb->IncFrequency(NAME_EVENT, oldIE->GetEvent(), -1);
		base->nb->IncFrequency(NAME_SITE, oldIE->GetSite(), -1);
		base->nb->IncFrequency(NAME_ROUND, oldIE->GetRound(), -1);
	}

	// Flush the gfile so it is up-to-date with other files:
	// This made copying games between databases VERY slow, so it
	// is now done elsewhere OUTSIDE a loop that copies many
	// games, such as in sc_filter_copy().
	//base->gfile->FlushAll();

	// Last of all, we write the new idxEntry, but NOT the index header
	// or the name file, since there might be more games saved yet and
	// writing them now would then be a waste of time.

	if (base->idx->WriteEntries(&iE, gNumber, 1) != OK)
	{
		return -13;
	}

	// We need to increase the filter size if a game was added:
	if (!replaceMode)
	{
		base->filter->Append(1); // Added game is in filter by default.
		if (base->filter != base->dbFilter)
			base->dbFilter->Append(1);
		if (base->treeFilter)
			base->treeFilter->Append(1);

		if (base->duplicates != NULL)
		{
			delete[] base->duplicates;
			base->duplicates = NULL;
		}
	}
	base->undoCurrent = base->undoIndex;
	base->undoCurrentNotAvail = false;

	return 0;
}

/// <summary>
/// Import: Imports games from a PGN file to the current base.
/// </summary>
/// <param name="numgames">returns number of games imported</param>
/// <param name="msgs">returns any PGN import errors or warnings</param>
/// <param name="pgnfile">the PGN file to import</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Base::Import(int% numgames, String^% msgs, String^ pgnfile)
{
	msclr::interop::marshal_context oMarshalContext;

	const char* pgn = oMarshalContext.marshal_as<const char*>(pgnfile);

	if (!db->inUse)
	{
		return -1;
	}
	// Cannot import into a read-only database unless it is memory-only:
	if (db->fileMode == FMODE_ReadOnly && !(db->memoryOnly))
	{
		return -2;
	}

	MFile pgnFile;
	uint inputLength = 0;
	PgnParser parser;

	if (pgnFile.Open(pgn, FMODE_ReadOnly) != OK)
	{
		return -3;
	}
	parser.Reset(&pgnFile);
	inputLength = fileSize(pgn, "");

	if (inputLength < 1)
	{
		inputLength = 1;
	}
	parser.IgnorePreGameText();
	uint gamesSeen = 0;

	while (parser.ParseGame(scratchGame) != ERROR_NotFound)
	{
		if (sc_savegame(scratchGame, 0, db) != 0)
		{
			// quick and nasty cleanup aka below
			db->gfile->FlushAll();
			pgnFile.Close();
			db->idx->WriteHeader();
			if (!db->memoryOnly)
				db->nb->WriteNameFile();
			recalcFlagCounts(db);
			if (!db->memoryOnly)
				removeFile(db->fileName, TREEFILE_SUFFIX);

			return -3;
		}
		gamesSeen++;
	}
	db->gfile->FlushAll();
	pgnFile.Close();

	// Now write the Index file header and the name file:
	if (db->idx->WriteHeader() != OK)
	{
		return -4;
	}
	if (!db->memoryOnly)
	{
		if (db->nb->WriteNameFile() != OK)
		{
			return -5;
		}
	}
	recalcFlagCounts(db);
	if (!db->memoryOnly)
	{
		removeFile(db->fileName, TREEFILE_SUFFIX);
	}

	numgames = gamesSeen;

	if (parser.ErrorCount() > 0)
	{
		msgs = gcnew System::String(parser.ErrorMessages());
	}
	else
	{
		msgs = gcnew System::String("");
	}

	return 0;
}

/// <summary>
/// CountFree: Return count of free base slots.
/// </summary>
/// <returns>count of free base slots</returns>
int ScincFuncs::Base::CountFree()
{
	int numFree = 0;
	for (int i = 0; i < MAX_BASES; i++)
	{
		if (!dbList[i].inUse)
		{
			numFree++;
		}
	}
	return numFree;
}

/// <summary>
/// Current: Returns the number of the current open database.
/// </summary>
/// <returns>the number of the current open database</returns>
int ScincFuncs::Base::Current()
{
	return currentBase + 1;
}


// CLIPBASE functions

/// <summary>
/// Clear: Clears the clipbase by closing and recreating it.
/// </summary>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Clipbase::Clear()
{
	if (!clipbase->inUse)
	{
		return -1;
	}
	clipbase->game->Clear();
	clipbase->nb->Clear();
	clipbase->gfile->Close();
	clipbase->gfile->CreateMemoryOnly();
	clipbase->idx->CloseIndexFile();
	clipbase->idx->CreateMemoryOnly();
	clipbase->idx->SetType(2);

	clipbase->numGames = 0;
	clearFilter(clipbase, clipbase->numGames);

	clipbase->inUse = true;
	clipbase->gameNumber = -1;
	clipbase->treeCache->Clear();
	clipbase->backupCache->Clear();
	recalcFlagCounts(clipbase);

	return 0;
}


// SCIDGAME functions

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//    resets data used for undos (for example after loading another game)
void sc_game_undo_reset()
{
	db->undoMax = -1;
	db->undoIndex = -1;
	db->undoCurrent = -1;
	db->undoCurrentNotAvail = false;
	for (int i = 0; i < UNDO_MAX; i++)
	{
		if (db->undoGame[i] != NULL)
		{
			delete db->undoGame[i];
			db->undoGame[i] = NULL;
		}
	}
}

/// <summary>
/// Load: Takes a game number and loads the game
/// </summary>
/// <param name="gnum">The game number</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::Load(unsigned int gnum)
{
	if (!db->inUse)
	{
		return -1;
	}

	sc_game_undo_reset();

	db->bbuf->Empty();

	// Check the game number is valid::
	if (gnum < 1 || gnum > db->numGames)
	{
		return -2;//Invalid game number
	}

	// We number games from 0 internally, so subtract one:
	gnum--;

	IndexEntry* ie = db->idx->FetchEntry(gnum);

	if (db->gfile->ReadGame(db->bbuf, ie->GetOffset(), ie->GetLength()) != OK)
	{
		return -3;//This game appears to be corrupt
	}
	if (db->game->Decode(db->bbuf, GAME_DECODE_ALL) != OK)
	{
		return -4;//This game appears to be corrupt
	}

	if (db->filter->Get(gnum) > 0)
	{
		db->game->MoveToPly(db->filter->Get(gnum) - 1);
	}
	else
	{
		db->game->MoveToPly(0);
	}

	db->game->LoadStandardTags(ie, db->nb);
	db->gameNumber = gnum;
	db->gameAltered = false;
	return 0;
}

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// sc_game_save:
//    Saves the current game. If the parameter is 0, a NEW
//    game is added; otherwise, that game number is REPLACED.

/// <summary>
/// Save: Saves the current game. If the parameter is 0, a NEW
/// game is added; otherwise, that game number is REPLACED.
/// </summary>
/// <param name="gnum">The game number</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::Save(unsigned int gnum)
{
	if (!db->inUse)
	{
		return -1;
	}
	if (db->fileMode == FMODE_ReadOnly)
	{
		return -2;
	}
	db->bbuf->Empty();

	if (gnum > db->numGames)
	{
		return -3;
	}

	db->game->SaveState();
	if (sc_savegame(db->game, gnum, db) != OK)
	{
		return -4;
	}
	db->gfile->FlushAll();
	db->game->RestoreState();
	if (db->idx->WriteHeader() != OK)
	{
		return -5;//Error writing index file
	}
	if (!db->memoryOnly && db->nb->WriteNameFile() != OK)
	{
		return -6;//Error writing name file
	}

	if (gnum == 0)
	{
		// Saved new game, so set gameNumber to the saved game number:
		db->gameNumber = db->numGames - 1;
	}
	db->gameAltered = false;

	// We must ensure that the Index is still all in memory:
	db->idx->ReadEntireFile();

	recalcFlagCounts(db);
	// Finally, saving a game makes the treeCache out of date:
	db->treeCache->Clear();
	db->backupCache->Clear();
	if (!db->memoryOnly)
	{
		removeFile(db->fileName, TREEFILE_SUFFIX);
	}

	return 0;
}

/// <summary>
/// StripComments: Strips all comments from a game.
/// </summary>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::StripComments()
{
	// we need to switch off short header style or PGN parsing will not work
	uint old_style = db->game->GetPgnStyle();
	uint old_ply = db->game->GetCurrentPly();
	if (old_style & PGN_STYLE_SHORT_HEADER)
		db->game->SetPgnStyle(PGN_STYLE_SHORT_HEADER, false);

	db->game->AddPgnStyle(PGN_STYLE_TAGS);
	db->game->AddPgnStyle(PGN_STYLE_COMMENTS);
	db->game->AddPgnStyle(PGN_STYLE_VARS);
	db->game->SetPgnFormat(PGN_FORMAT_Plain);

	db->game->RemovePgnStyle(PGN_STYLE_COMMENTS);

	db->tbuf->Empty();
	db->tbuf->SetWrapColumn(99999);
	db->game->WriteToPGN(db->tbuf);
	PgnParser parser;
	parser.Reset((const char*)db->tbuf->GetBuffer());
	scratchGame->Clear();
	if (parser.ParseGame(scratchGame))
	{
		return -1;//Error: unable to strip this game.
	}
	parser.Reset((const char*)db->tbuf->GetBuffer());
	db->game->Clear();
	parser.ParseGame(db->game);

	// Restore PGN style (Short header)
	if (old_style & PGN_STYLE_SHORT_HEADER)
		db->game->SetPgnStyle(PGN_STYLE_SHORT_HEADER, true);

	db->game->MoveToPly(old_ply);
	db->gameAltered = true;
	return 0;
}

/// <summary>
/// GetTag: Gets a tag for the active game given its name.
/// Valid names are:  Event, Site, Date, Round, White, Black, ECO, EDate
/// </summary>
/// <param name="tag">Tag as a string, such as White</param>
/// <param name="val">Value returned as a string</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::GetTag(String^ tag, String^% val)
{
	msclr::interop::marshal_context oMarshalContext;

	const char* tg = oMarshalContext.marshal_as<const char*>(tag);

	static const char* options[] = {
		"Event", "Site", "Date", "Round", 
		"White", "Black", "ECO", "EDate", 
		NULL };
	enum
	{
		T_Event,
		T_Site,
		T_Date,
		T_Round,
		T_White,
		T_Black,
		T_ECO,
		T_EDate
	};

	Game* g = db->game;

	const char* s;
	int index = strExactMatch(tg, options);

	switch (index)
	{
	case T_Event:
		s = g->GetEventStr();
		if (!s)
		{
			s = "?";
		}
		val = gcnew System::String(s);
		break;

	case T_Site:
		s = g->GetSiteStr();
		if (!s)
		{
			s = "?";
		}
		val = gcnew System::String(s);
		break;

	case T_Date:
		char dateStr[20];
		date_DecodeToString(g->GetDate(), dateStr);
		val = gcnew System::String(dateStr);
		break;

	case T_Round:
		s = g->GetRoundStr();
		if (!s)
		{
			s = "?";
		}
		val = gcnew System::String(s);
		break;

	case T_White:
		s = g->GetWhiteStr();
		if (!s)
		{
			s = "?";
		}
		val = gcnew System::String(s);
		break;

	case T_Black:
		s = g->GetBlackStr();
		if (!s)
		{
			s = "?";
		}
		val = gcnew System::String(s);
		break;

	case T_ECO:
		ecoStringT ecoStr;
		eco_ToExtendedString(g->GetEco(), ecoStr);
		val = gcnew System::String(ecoStr);
		break;

	case T_EDate:
		char edateStr[20];
		date_DecodeToString(g->GetEventDate(), edateStr);
		val = gcnew System::String(dateStr);
		break;

	default: // Not a valid tag name.
		return -1;
	}

	return 0;
}

/// <summary>
/// SetTag: Set a standard tag for this game.
/// </summary>
/// <param name="tag">Tag as a string, such as White</param>
/// <param name="val">Value as a string</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::SetTag(String^ tag, String^ val)
{
	msclr::interop::marshal_context oMarshalContext;

	const char* tg = oMarshalContext.marshal_as<const char*>(tag);
	const char* value = oMarshalContext.marshal_as<const char*>(val);

	static const char* options[] = {
	"Event", "Site", "Date", "Round", "White", "Black", "Result",
	"WhiteElo", "WhiteRType", "BlackElo", "BlackRType",
	"ECO", "EDate",
	NULL };
	enum
	{
		T_EVENT,
		T_SITE,
		T_DATE,
		T_ROUND,
		T_WHITE,
		T_BLACK,
		T_RESULT,
		T_WHITE_ELO,
		T_WHITE_RTYPE,
		T_BLACK_ELO,
		T_BLACK_RTYPE,
		T_ECO,
		T_EVENTDATE,
	};

	int index = strUniqueMatch(tg, options);
		
	switch (index)
	{
	case T_EVENT:
		db->game->SetEventStr(value);
		break;
	case T_SITE:
		db->game->SetSiteStr(value);
		break;
	case T_DATE:
		db->game->SetDate(date_EncodeFromString(value));
		break;
	case T_ROUND:
		db->game->SetRoundStr(value);
		break;
	case T_WHITE:
		db->game->SetWhiteStr(value);
		break;
	case T_BLACK:
		db->game->SetBlackStr(value);
		break;
	case T_RESULT:
		db->game->SetResult(strGetResult(value));
		break;
	case T_WHITE_ELO:
		db->game->SetWhiteElo(strGetUnsigned(value));
		break;
	case T_WHITE_RTYPE:
		db->game->SetWhiteRatingType(strGetRatingType(value));
		break;
	case T_BLACK_ELO:
		db->game->SetBlackElo(strGetUnsigned(value));
		break;
	case T_BLACK_RTYPE:
		db->game->SetBlackRatingType(strGetRatingType(value));
		break;
	case T_ECO:
		db->game->SetEco(eco_FromString(value));
		break;
	case T_EVENTDATE:
		db->game->SetEventDate(date_EncodeFromString(value));
		break;
	default:
		return -1;
	}

	return 0;
}

/// <summary>
/// List: Returns a portion of the game list according to the current filter.
/// Takes start and count, where start is in the range (1..FilterCount).
/// The next argument is the format string -- -- see index.cpp for details.,
/// </summary>
/// <param name="glist">the game list returned</param>
/// <param name="start">the gane number to start from</param>
/// <param name="count">the number of games to get</param>
/// <param name="formatStr">the format string</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::List(String^% glist,unsigned int start, unsigned int count, String^ formatStr)
{
	msclr::interop::marshal_context oMarshalContext;

	const char* format = oMarshalContext.marshal_as<const char*>(formatStr);
	
	if (!db->inUse)
	{
		return 0;
	}

	if (start < 1 || start > db->numGames)
	{
		return 0;
	}
	uint fcount = db->filter->Count();
	if (fcount > count)
	{
		fcount = count;
	}

	uint index = db->filter->FilteredCountToIndex(start);
	IndexEntry* ie;
	char temp[2048];
	glist = gcnew System::String("");

	while (index < db->numGames && count > 0)
	{
		if (db->filter->Get(index))
		{
			ie = db->idx->FetchEntry(index);
			ie->PrintGameInfo(temp, start, index + 1, db->nb, format);
			// separate lines by newline &&& Issues ?
			glist = glist + gcnew System::String(temp) + gcnew System::String("\n");
			count--;
			start++;
		}
		index++;
	}
	return 0;
}

/// <summary>
/// Pgn: Returns the PGN representation of the game.
/// </summary>
/// <param name="pgn">string that will hold the pgen representation</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::Pgn(String^% pgn)
{
	scidBaseT* base = db;
	Game* g = db->game;
	uint lineWidth = 99999;
	g->ResetPgnStyle();
	g->SetPgnFormat(PGN_FORMAT_Plain);
	g->AddPgnStyle(PGN_STYLE_TAGS | PGN_STYLE_COMMENTS | PGN_STYLE_VARS);
	base->tbuf->Empty();
	base->tbuf->SetWrapColumn(lineWidth);
	g->WriteToPGN(base->tbuf);
	pgn = gcnew System::String(base->tbuf->GetBuffer());
	return 0;
}

// ECO functions

/// <summary>
/// Read: Reads a book file for ECO classification.
/// </summary>
/// <param name="econm">the ECO file name</param>
/// <returns>returns the book size (number of positions)</returns>
int ScincFuncs::Eco::Read(String^ econm)
{
	msclr::interop::marshal_context oMarshalContext;

	const char* econame = oMarshalContext.marshal_as<const char*>(econm);

	if (ecoBook)
	{
		delete ecoBook;
	}
	ecoBook = new PBook;
	ecoBook->SetFileName(econame);
	errorT err = ecoBook->ReadEcoFile();
	if (err != OK)
	{
		delete ecoBook;
		ecoBook = NULL;
		return -1;
	}
	return ecoBook->Size();
}

/// <summary>
/// Base: Reclassifies all games in the current base by ECO code.
/// </summary>
/// <param name="msgs">The messages returned</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Eco::Base(String^% msgs)
{
	if (!ecoBook)
	{
		return -1;//No ECO Book is loaded
	}
	if (!db->inUse)
	{
		return -2;
	}

	bool extendedCodes = true;
	Game* g = scratchGame;
	IndexEntry* ie;
	uint updateStart, update;
	updateStart = update = 1000; // Update progress bar every 1000 games
	errorT err = OK;
	uint countClassified = 0; // Count of games classified.
	dateT startDate = ZERO_DATE;

	Timer timer; // Time the classification operation.

	// Read each game:
	for (uint i = 0; i < db->numGames; i++)
	{
		ie = db->idx->FetchEntry(i);
		if (ie->GetLength() == 0)
		{
			continue;
		}
		ecoT oldEcoCode = ie->GetEcoCode();
		if (db->gfile->ReadGame(db->bbuf, ie->GetOffset(),
			ie->GetLength()) != OK)
		{
			continue;
		}
		db->bbuf->BackToStart();
		g->Clear();
		if (g->DecodeStart(db->bbuf) != OK)
		{
			continue;
		}

		// First, read in the game -- with a limit of 30 moves per
		// side, since an ECO match after move 31 is very unlikely and
		// we can save time by setting a limit. Also, stop when the
		// material left in on the board is less than that of the
		// book position with the least material, since no further
		// positions in the game could possibly match.

		uint maxPly = 60;
		uint leastMaterial = ecoBook->FewestPieces();
		uint material;

		do
		{
			err = g->DecodeNextMove(db->bbuf, NULL);
			maxPly--;
			material = g->GetCurrentPos()->TotalMaterial();
		} while (err == OK && maxPly > 0 && material >= leastMaterial);

		// Now, move back through the game to the start searching for a
		// match in the ECO book. Stop at the first match found since it
		// is the deepest.

		DString commentStr;
		bool found = false;

		do
		{
			if (ecoBook->FindOpcode(g->GetCurrentPos(), "eco",
				&commentStr) == OK)
			{
				found = true;
				break;
			}
			err = g->MoveBackup();
		} while (err == OK);

		ecoT ecoCode = ECO_None;
		if (found)
		{
			ecoCode = eco_FromString(commentStr.Data());
			if (!extendedCodes)
			{
				ecoCode = eco_BasicCode(ecoCode);
			}
		}
		ie->SetEcoCode(ecoCode);
		countClassified++;

		// If the database is read-only or the ECO code has not changed,
		// nothing needs to be written to the index file.
		// Write the updated entry if necessary:

		if (db->fileMode != FMODE_ReadOnly && ie->GetEcoCode() != oldEcoCode)
		{
			if (db->idx->WriteEntries(ie, i, 1) != OK)
			{
				return -3;//Error writing index file
			}
		}
	}

	// Update the index file header:
	if (db->fileMode != FMODE_ReadOnly)
	{
		if (db->idx->WriteHeader() != OK)
		{
			return -4;//Error writing index file
		}
	}

	recalcFlagCounts(db);

	int centisecs = timer.CentiSecs();
	char tempStr[100];
	sprintf(tempStr, "Classified %u game%s in %d%c%02d seconds",
		countClassified, strPlural(countClassified),
		centisecs / 100, decimalPointChar, centisecs % 100);
	msgs = gcnew System::String(tempStr);
	return 0;
}


// POS functions
// DONT USE AS HANDLED IN F#

// FILT functions

/// <summary>
/// Count: returns the current filter size
/// </summary>
/// <returns>the current filter size</returns>
int ScincFuncs::Filt::Count()
{
	scidBaseT* basePtr = db;
	return basePtr->inUse ? basePtr->filter->Count() : 0;
}

// Allow for a second filter when the tree window is open
// When tree is closed, things look like this:
//     db->filter = new Filter(0);
//     db->dbFilter = db->filter;
//     db->treeFilter = NULL;
// When the tree is open && "Adjust Filter" is selected, it performs a filter
// AND with the tree position... Quite handy ;>
void updateMainFilter(scidBaseT* dbase)
{
	if (dbase->dbFilter != dbase->filter)
	{
		for (uint i = 0; i < dbase->numGames; i++)
		{
			if (dbase->dbFilter->Get(i) > 0 && dbase->treeFilter->Get(i) > 0)
				dbase->filter->Set(i, dbase->treeFilter->Get(i));
			else
				dbase->filter->Set(i, 0);
		}
	}
}

// Same as above, but only update non-zero values for their ply
void updateMainFilter2(scidBaseT* dbase)
{
	if (dbase->dbFilter != dbase->filter)
	{
		for (uint i = 0; i < dbase->numGames; i++)
		{
			if (dbase->dbFilter->Get(i) > 0 && dbase->treeFilter->Get(i) > 0)
				dbase->filter->Set(i, dbase->treeFilter->Get(i));
		}
	}
}

// TREE functions

// Enumeration of possible move-sorting methods for tree mode:
enum moveSortE
{
	SORT_ALPHA,
	SORT_ECO,
	SORT_FREQUENCY,
	SORT_SCORE
};

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// sortTreeMoves():
//    Sorts the moves of a tree node according to a specified order.
//    Slow sort method, but the typical number of moves is under 20
//    so it is easily fast enough.
void sortTreeMoves(treeT* tree, int sortMethod, colorT toMove)
{
	// Only sort if there are at least two moves in the tree node:
	if (tree->moveCount <= 1)
	{
		return;
	}

	for (uint outer = 0; outer < tree->moveCount - 1; outer++)
	{
		for (uint inner = outer + 1; inner < tree->moveCount; inner++)
		{
			int result = 0;

			switch (sortMethod)
			{
			case SORT_FREQUENCY: // Most frequent moves first:
				result = tree->node[outer].total - tree->node[inner].total;
				break;

			case SORT_ALPHA: // Alphabetical order:
				result = strCompare(tree->node[inner].san,
					tree->node[outer].san);
				break;

			case SORT_ECO: // ECO code order:
				result = tree->node[outer].ecoCode - tree->node[inner].ecoCode;
				break;

			case SORT_SCORE: // Order by success:
				result = tree->node[outer].score - tree->node[inner].score;
				if (toMove == BLACK)
				{
					result = -result;
				}
				break;

			default: // Unreachable:
				return;
			}

			if (result < 0)
			{
				// Swap the nodes:
				treeNodeT temp = tree->node[outer];
				tree->node[outer] = tree->node[inner];
				tree->node[inner] = temp;
			}
		} // for (inner)
	}     // for (outer)
	return;
}

/// <summary>
/// Search: Gets the stats tree for the current database
/// </summary>
/// <param name="treestr">the stats tree returned</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Tree::Search(String^ fenstr, String^% treestr)
{
	msclr::interop::marshal_context oMarshalContext;

	const char* fen = oMarshalContext.marshal_as<const char*>(fenstr);
	
	char tempTrans[10];

	scidBaseT* base = db;
	db->bbuf->Empty();

	static std::set<scidBaseT**> search_pool;

	if (!base->inUse)
	{
		return -1;
	}

	search_pool.insert(&base);

	if (base->treeFilter == NULL)
	{
		base->dbFilter = base->filter->Clone();
		base->treeFilter = new Filter(base->numGames);
	}

	// Set vars
	Position* pos = db->game->GetCurrentPos();
	//set to fen
	pos->ReadFromFEN(fen);

	treeT* tree = &(base->tree);
	tree->moveCount = tree->totalCount = 0;
	matSigT msig = matsig_Make(pos->GetMaterial());
	uint hpSig = pos->GetHPSig();
	simpleMoveT sm;
	base->treeFilter->Fill(0); // Reset the filter to be empty
	uint skipcount = 0;

	// Set up the stored line code matches:
	StoredLine stored_line(pos);

	// Search through each game:
	for (uint i = 0; i < base->numGames; i++)
	{
		const byte* oldFilterData = base->treeFilter->GetOldDataTree();

		IndexEntry* ie = base->idx->FetchEntry(i);
		if (ie->GetLength() == 0)
		{
			skipcount++;
			continue;
		}

		bool foundMatch = false;
		uint ply = 0;

		// Check the stored line result for this game:
		if (!stored_line.CanMatch(ie->GetStoredLineCode(), &ply, &sm))
		{
			skipcount++;
			continue;
		}
		if (ply > 0)
			foundMatch = true;

		pieceT* bd = pos->GetBoard();
		bool isStartPos =
			(bd[A1] == WR && bd[B1] == WN && bd[C1] == WB && bd[D1] == WQ &&
				bd[E1] == WK && bd[F1] == WB && bd[G1] == WN && bd[H1] == WR &&
				bd[A2] == WP && bd[B2] == WP && bd[C2] == WP && bd[D2] == WP &&
				bd[E2] == WP && bd[F2] == WP && bd[G2] == WP && bd[H2] == WP &&
				bd[A7] == BP && bd[B7] == BP && bd[C7] == BP && bd[D7] == BP &&
				bd[E7] == BP && bd[F7] == BP && bd[G7] == BP && bd[H7] == BP &&
				bd[A8] == BR && bd[B8] == BN && bd[C8] == BB && bd[D8] == BQ &&
				bd[E8] == BK && bd[F8] == BB && bd[G8] == BN && bd[H8] == BR);

		if (!isStartPos && ie->GetNumHalfMoves() == 0)
		{
			skipcount++;
			continue;
		}

		if (!foundMatch && !ie->GetStartFlag())
		{
			// Speedups that only apply to standard start games:
			if (hpSig != HPSIG_StdStart)
			{ // Not the start mask
				if (!hpSig_PossibleMatch(hpSig, ie->GetHomePawnData()))
				{
					skipcount++;
					continue;
				}
			}
		}

		if (!foundMatch && msig != MATSIG_StdStart && !matsig_isReachable(msig, ie->GetFinalMatSig(), ie->GetPromotionsFlag(), ie->GetUnderPromoFlag()))
		{
			skipcount++;
			continue;
		}

		if (!foundMatch)
		{
			if (base->gfile->ReadGame(base->bbuf, ie->GetOffset(),
				ie->GetLength()) != OK)
			{
				search_pool.erase(&base);
				return -2;//Error reading game file
			}
			Game* g = scratchGame;
			if (g->ExactMatch(pos, base->bbuf, &sm))
			{
				ply = g->GetCurrentPly() + 1;
				//if (ply > 255) { ply = 255; }
				foundMatch = true;
				//base->filter->Set (i, (byte) ply);
			}
		}

		// If match was found, add it to the list of found moves:
		if (foundMatch)
		{
			if (ply > 255)
			{
				ply = 255;
			}
			base->treeFilter->Set(i, (byte)ply);
			uint search;
			treeNodeT* node = tree->node;
			for (search = 0; search < tree->moveCount; search++, node++)
			{
				if (sm.from == node->sm.from && sm.to == node->sm.to && sm.promote == node->sm.promote)
				{
					break;
				}
			}

			// Now node is the node to update or add.
			// Check for exceeding max number of nodes:
			if (search >= MAX_TREE_NODES)
			{
				search_pool.erase(&base);
				return -3;//Too many moves
			}

			if (search == tree->moveCount)
			{
				// A new move to add:
				initTreeNode(node);
				node->sm = sm;
				if (sm.from == NULL_SQUARE)
				{
					strCopy(node->san, "[end]");
				}
				else
				{
					pos->MakeSANString(&sm, node->san, SAN_CHECKTEST);
				}
				tree->moveCount++;
			}
			node->total++;
			node->freq[ie->GetResult()]++;
			eloT elo = 0;
			eloT oppElo = 0;
			uint year = ie->GetYear();
			if (pos->GetToMove() == WHITE)
			{
				elo = ie->GetWhiteElo();
				oppElo = ie->GetBlackElo();
			}
			else
			{
				elo = ie->GetBlackElo();
				oppElo = ie->GetWhiteElo();
			}
			if (elo > 0)
			{
				node->eloSum += elo;
				node->eloCount++;
			}
			if (oppElo > 0)
			{
				node->perfSum += oppElo;
				node->perfCount++;
			}
			if (year != 0)
			{
				node->yearSum += year;
				node->yearCount++;
			}
			tree->totalCount++;
		} 
	}     

	updateMainFilter2(base);

	// Now we generate the score of each move: it is the expected score per
	// 1000 games. Also generate the ECO code of each move.

	DString dstr;
	treeNodeT* node = tree->node;
	for (uint i = 0; i < tree->moveCount; i++, node++)
	{
		node->score = (node->freq[RESULT_White] * 2 + node->freq[RESULT_Draw] + node->freq[RESULT_None]) * 500 / node->total;

		node->ecoCode = 0;
		if (ecoBook != NULL)
		{
			scratchPos->CopyFrom(db->game->GetCurrentPos());
			if (node->sm.from != NULL_SQUARE)
			{
				scratchPos->DoSimpleMove(&(node->sm));
			}
			dstr.Clear();
			if (ecoBook->FindOpcode(scratchPos, "eco", &dstr) == OK)
			{
				node->ecoCode = eco_FromString(dstr.Data());
			}
		}
	}

	// Now we sort the move list:
	sortTreeMoves(tree, SORT_FREQUENCY, db->game->GetCurrentPos()->GetToMove());

	search_pool.erase(&base);

	DString* output = new DString;
	char temp[200];

	// Now we print the list into the return string:
	node = tree->node;
	for (uint count = 0; count < tree->moveCount; count++, node++)
	{
		ecoStringT ecoStr;
		eco_ToExtendedString(node->ecoCode, ecoStr);
		uint avgElo = 0;
		if (node->eloCount >= 10)
		{
			// bool foundInCache = false;
			avgElo = node->eloSum / node->eloCount;
		}
		uint perf = 0;
		if (node->perfCount >= 10)
		{
			perf = node->perfSum / node->perfCount;
			uint score = (node->score + 5) / 10;
			if (db->game->GetCurrentPos()->GetToMove() == BLACK)
			{
				score = 100 - score;
			}
		}
		unsigned long long avgYear = 0;
		if (node->yearCount > 0)
		{
			avgYear = (node->yearSum + (node->yearCount / 2)) / node->yearCount;
		}
		node->san[6] = 0;

		strcpy(tempTrans, node->san);

		sprintf(temp, "%2u|%-6s|%7u|%3u%c%1u%|%3u%c%1u%",
			count + 1,
			tempTrans, //node->san,
			node->total,
			100 * node->total / tree->totalCount,
			decimalPointChar,
			(1000 * node->total / tree->totalCount) % 10,
			node->score / 10,
			decimalPointChar,
			node->score % 10);
		output->Append(temp);
		uint pctDraws = node->freq[RESULT_Draw] * 1000 / node->total;
		sprintf(temp, "|%3u%", (pctDraws + 5) / 10);
		output->Append(temp);

		sprintf(temp, "|%4u", avgElo);
		output->Append(temp);
		sprintf(temp, "|%4u", perf);
		output->Append(temp);
		sprintf(temp, "|%4llu", avgYear);
		output->Append(temp);
		sprintf(temp, "|%-5s\n", ecoStr);
		output->Append(temp);
	}

	// Print a totals line as well, if there are any moves in the tree:

	if (tree->moveCount > 0)
	{
		int totalScore = 0;
		unsigned long long eloSum = 0;
		unsigned long long eloCount = 0;
		unsigned long long perfSum = 0;
		unsigned long long perfCount = 0;
		unsigned long long yearCount = 0;
		unsigned long long yearSum = 0;
		uint nDraws = 0;
		node = tree->node;
		for (uint count = 0; count < tree->moveCount; count++, node++)
		{
			totalScore += node->freq[RESULT_White] * 2;
			totalScore += node->freq[RESULT_Draw] + node->freq[RESULT_None];
			eloCount += node->eloCount;
			eloSum += node->eloSum;
			perfCount += node->perfCount;
			perfSum += node->perfSum;
			yearCount += node->yearCount;
			yearSum += node->yearSum;
			nDraws += node->freq[RESULT_Draw];
		}
		totalScore = totalScore * 500 / tree->totalCount;
		unsigned long long avgElo = 0;
		if (eloCount >= 10)
		{
			avgElo = eloSum / eloCount;
		}
		uint perf = 0;
		if (perfCount >= 10)
		{
			perf = perfSum / perfCount;
		}

		sprintf(temp, "%7u|%3d%c%1d%",
			tree->totalCount, totalScore / 10, decimalPointChar, totalScore % 10);
		output->Append(temp);
		uint pctDraws = nDraws * 1000 / tree->totalCount;
		sprintf(temp, "|%3u%", (pctDraws + 5) / 10);
		output->Append(temp);
		sprintf(temp, "|%4llu", avgElo);
		output->Append(temp);
		sprintf(temp, "|%4u", perf);
		output->Append(temp);
		sprintf(temp, "|%4llu", (yearSum + (yearCount / 2)) / yearCount);
		output->Append(temp);
	}

	treestr = gcnew System::String(output->Data());
	delete output;

	return 0;
}
