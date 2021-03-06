
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

/// <summary>
/// Switch: Switch to a different database slot
/// </summary>
/// <param name="basenum">the slot to switch to</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Base::Switch(int basenum)
{
	if (basenum < 1 || basenum > MAX_BASES)
	{
		return -1; //Invalid base number
	}

	currentBase = basenum - 1;
	db = &(dbList[currentBase]);
	return 0;
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

/// <summary>
/// Save: Saves the current game. If the parameter is 0, a NEW
/// game is added; otherwise, that game number is REPLACED.
/// </summary>
/// <param name="gnum">The game number</param>
/// <param name="basenum">the base containing the game</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::Save(unsigned int gnum, int basenum)
{
	if (basenum < 1 || basenum > MAX_BASES)
	{
		return -1; //Invalid base number
	}

	currentBase = basenum - 1;
	db = &(dbList[currentBase]);
	
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
/// Delete: sets the delete flag for the game number
/// </summary>
/// <param name="gnum">the game number to change</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::Delete(unsigned int gnum)
{
	if (!db->inUse)
	{
		return -1;
	}
	if (db->fileMode == FMODE_ReadOnly)
	{
		return -2;
	}
	uint gNum = gnum - 1;
	uint flagType = 1 << IDX_FLAG_DELETE;
	IndexEntry* ie = db->idx->FetchEntry(gNum);
	IndexEntry iE = *ie;
	iE.SetFlag(flagType, 1);
	db->idx->WriteEntries(&iE, gNum, 1);

	return 0;
}

/// <summary>
/// SavePgn: Saves the current game using the PGN string. 
/// If the game number parameter is 0, a NEW
/// game is added; otherwise, that game number is REPLACED.
/// </summary>
/// <param name="pgnstr">the string holding the PGN of the game</param>
/// <param name="gnum">The game number</param>
/// <param name="basenum">the base containing the game</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::SavePgn(String^ pgnstr, unsigned int gnum, int basenum)
{
	msclr::interop::marshal_context oMarshalContext;

	const char* pgn = oMarshalContext.marshal_as<const char*>(pgnstr);

	if (basenum < 1 || basenum > MAX_BASES)
	{
		return -1; //Invalid base number
	}

	currentBase = basenum - 1;
	db = &(dbList[currentBase]);
	
	PgnParser parser(pgn);
	errorT err = parser.ParseGame(db->game);
	db->gameAltered = true;
	if (err == ERROR_NotFound)
	{
		return -2;//ERROR: No PGN header tag 
	}
	return ScincFuncs::ScidGame::Save(gnum,basenum);
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
		"White", "Black", "ECO", "EDate", "WhiteElo", "BlackElo", "Result",
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
		T_EDate,
		T_WhiteElo,
		T_BlackElo,
		T_Result
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

	case T_WhiteElo:
		char weloStr[20];
		sprintf(weloStr, "%u", g->GetWhiteElo());
		val = gcnew System::String(weloStr);
		break;

	case T_BlackElo:
		char beloStr[20];
		sprintf(beloStr, "%u", g->GetBlackElo());
		val = gcnew System::String(beloStr);
		break;

	case T_Result:
		char resStr[20];
		sprintf(resStr, "%u", g->GetResult());
		val = gcnew System::String(resStr);
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
/// GetFen: getfens for game if non-standard start
/// </summary>
/// <param name="fen">fen returned, empty if standard start</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::GetFen(String^% fen)
{
	char boardStr[200];
	if (db->game->HasNonStandardStart())
	{
		db->game->GetStartPos()->PrintFEN(boardStr, FEN_ALL_FIELDS);
		fen = gcnew System::String(boardStr);
		return 0;
	}
	return 0;
}

/// <summary>
/// HasNonStandardStart: returns whether has a FEN, so does not start from normal start position
/// </summary>
/// <returns>returns true or false</returns>
bool ScincFuncs::ScidGame::HasNonStandardStart()
{
	return db->game->HasNonStandardStart();
}

/// <summary>
/// GetMoves: Gets a list of the moves up to the specified maximum ply
/// </summary>
/// <param name="mvs">the list of moves returned</param>
/// <param name="maxply">the maximu ply to use, if set to -1 returns all moves</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::GetMoves(System::Collections::Generic::List<String^>^% mvs, int maxply)
{
	char sanStr[20];
	uint plyCount = 0;
	int limply = maxply == -1 ? 400 : maxply;

	db->game->SaveState();
	db->game->MoveToPly(0);
	do
	{
		simpleMoveT* sm = db->game->GetCurrentMove();
		db->game->GetSAN(sanStr);
		String^ mv = gcnew System::String(sanStr);
		if (mv!="") mvs->Add(mv);
		plyCount++;

	} while ((db->game->MoveForward() == OK) && plyCount<maxply);
	db->game->RestoreState();
	return 0;
}

/// <summary>
/// GetMovesPosns: Gets a list of the moves and postions up to the specified maximum ply
/// </summary>
/// <param name="mvs">the list of moves returned</param>
/// <param name="posns">the list of posns returned</param>
/// <param name="maxply">the maximu ply to use, if set to -1 returns all moves</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::GetMovesPosns(System::Collections::Generic::List<String^>^% mvs, System::Collections::Generic::List<String^>^% posns, int maxply)
{
	char sanStr[20];
	char boardStr[200];
	uint plyCount = 0;
	int limply = maxply == -1 ? 400 : maxply;

	db->game->SaveState();
	db->game->MoveToPly(0);
	do
	{
		simpleMoveT* sm = db->game->GetCurrentMove();
		db->game->GetSAN(sanStr);
		String^ mv = gcnew System::String(sanStr);
		if (mv != "") mvs->Add(mv);
		db->game->GetCurrentPos()->MakeLongStr(boardStr);
		String^ bd = gcnew System::String(boardStr);
		if (bd != "" && mv != "") posns->Add(bd);
		plyCount++;

	} while ((db->game->MoveForward() == OK) && plyCount < maxply);
	db->game->RestoreState();
	return 0;
}


/// <summary>
/// List: Returns a portion of the game list according to the current filter.
/// Takes start and count, where start is in the range (1..FilterCount).
/// </summary>
/// <param name="gms">the game list returned</param>
/// <param name="start">the gane number to start from</param>
/// <param name="count">the number of games to get</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::ScidGame::List(System::Collections::Generic::List<gmui^>^% gms, unsigned int start, unsigned int count)
{
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
	
	while (index < db->numGames && count > 0)
	{
		if (db->filter->Get(index))
		{
			ie = db->idx->FetchEntry(index);
			//create struct
			gmui^ gm = gcnew gmui;
			gm->Num = index+1;
			gm->White = gcnew System::String(ie->GetWhiteName(db->nb));
			gm->Black = gcnew System::String(ie->GetBlackName(db->nb));
			gm->Result = gcnew System::String((char*)RESULT_STR[ie->GetResult()]);
			gm->Length = (ie->GetNumHalfMoves() + 1) / 2;
			date_DecodeToString(ie->GetDate(), temp);
			gm->Date = gcnew System::String(temp);
			gm->Event = gcnew System::String(ie->GetEventName(db->nb));
			gm->W_Elo = ie->GetWhiteElo();
			gm->B_Elo = ie->GetBlackElo();
			gm->Round = ie->GetRound();
			gm->Site = gcnew System::String(ie->GetSiteName(db->nb));
			gm->Deleted = gcnew System::String((ie->GetFlag(IDX_MASK_DELETE))?"D":"");
			gm->Variations = ie->GetVariationCount();
			gm->Comments = ie->GetCommentCount();
			gm->Annos = ie->GetNagCount();
			eco_ToExtendedString(ie->GetEcoCode(), temp);
			gm->ECO = gcnew System::String(temp);
			gm->Opening = gcnew System::String(StoredLine::GetText(ie->GetStoredLineCode()));
			ie->GetFlagStr(temp, NULL);
			gm->Flags = gcnew System::String(temp);
			gm->Start = gcnew System::String((ie->GetFlag(IDX_MASK_START)) ? "S" : "");
			int ln = gm->Site->Length;
			
			gms->Add(gm);

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
/// ScidGame: Returns ECO code for the curent game.
/// </summary>
/// <param name="eco">the ECO code</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Eco::ScidGame(String^% eco)
{
	int found = 0;
	uint ply = 0;
	if (!ecoBook)
	{
		return 0;
	}

	db->game->SaveState();
	db->game->MoveToPly(0);
	DString ecoStr;

	do
	{
	} while (db->game->MoveForward() == OK);
	do
	{
		if (ecoBook->FindOpcode(db->game->GetCurrentPos(), "eco",
			&ecoStr) == OK)
		{
			found = 1;
			ply = db->game->GetCurrentPly();
			break;
		}
	} while (db->game->MoveBackup() == OK);

	if (found)
	{
		ecoT ecoCode = eco_FromString(ecoStr.Data());
		ecoStringT eecoStr;
		eco_ToExtendedString(ecoCode, eecoStr);
		eco = gcnew System::String(eecoStr);
	}
	db->game->RestoreState();
	return 0;
}

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
	errorT err = OK;
	uint countClassified = 0; // Count of games classified.
	dateT startDate = ZERO_DATE;

	
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

	char tempStr[100];
	sprintf(tempStr, "Classified %u game%s",
		countClassified, strPlural(countClassified));
	msgs = gcnew System::String(tempStr);
	return 0;
}


// POS functions
// DONT USE AS HANDLED IN F#

// FILT functions

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// filter_reset:
//    Resets the filter of the specified base to include all or no games.
void filter_reset(scidBaseT* base, byte value)
{
	if (base->inUse)
	{
		base->dbFilter->Fill(value);
		if (base->dbFilter != base->filter)
			base->filter->Fill(value);
	}
}

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

void setMainFilter(scidBaseT* dbase)
{
	if (dbase->filter != dbase->dbFilter)
	{
		for (uint i = 0; i < dbase->numGames; i++)
		{
			dbase->filter->Set(i, dbase->dbFilter->Get(i));
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
/// treeSearch: Local function to gets the stats tree for the specified position for the specified database 
/// </summary>
/// <param name="mvsts">the class holding the move line data</param>
/// <param name="tsts">the class holding the total line data</param>
/// <param name="fenstr">the fen of current position</param>
/// <param name="basenum">the number of the base</param>
/// <returns>returns 0 if successful</returns>
int treeSearch(System::Collections::Generic::List<ScincFuncs::mvstats^>^% mvsts, ScincFuncs::totstats^% tsts, String^ fenstr, int basenum, bool returnTree)
{
	msclr::interop::marshal_context oMarshalContext;

	const char* fen = oMarshalContext.marshal_as<const char*>(fenstr);

	Position* pos = new Position;
	pos->ReadFromFEN(fen);
	
	char tempTrans[10];

	scidBaseT* base = db;
	db->bbuf->Empty();

	if (basenum >= 1 && basenum <= MAX_BASES)
	{
		base = &(dbList[basenum - 1]);
	}

	static std::set<scidBaseT**> search_pool;

	if (!base->inUse)
	{
		return -1;
	}
	search_pool.clear();
	search_pool.insert(&base);

	if (base->treeFilter == NULL)
	{
		base->dbFilter = base->filter->Clone();
		base->treeFilter = new Filter(base->numGames);
	}

	// 1. Cache Search
	bool foundInCache = false;
	// Check if there is a TreeCache file to open:
	base->treeCache->ReadFile(base->fileName);

	// Lookup the cache before searching:
	cachedTreeT* pct = base->treeCache->Lookup(pos);
	if (pct != NULL)
	{
		// It was in the cache! Use it to save time:
		if (pct->cfilter->Size() == base->numGames)
		{
			if (pct->cfilter->UncompressTo(base->treeFilter) == OK)
			{
				base->tree = pct->tree;

				foundInCache = true;
			}
		}
	}

	treeT* tree = &(base->tree);
	if (!foundInCache)
	{
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
			//const byte* oldFilterData = base->treeFilter->GetOldDataTree();

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

	}

	updateMainFilter(base);

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
			scratchPos->CopyFrom(pos);
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
	sortTreeMoves(tree, SORT_FREQUENCY, pos->GetToMove());

	//update cache
	if (!foundInCache)
	{
		base->treeCache->Add(pos, tree, base->treeFilter);
	}

	search_pool.erase(&base);

	// Now we return the move list if returnTree is true
	if (returnTree)
	{
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
				if (pos->GetToMove() == BLACK)
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

			ScincFuncs::mvstats^ mvst = gcnew ScincFuncs::mvstats();
			mvst->Mvstr = gcnew System::String(tempTrans);
			mvst->Count = node->total;
			mvst->Freq = static_cast<double>(node->total) / tree->totalCount;
			mvst->WhiteWins = node->freq[RESULT_White];
			mvst->Draws = node->freq[RESULT_Draw];
			mvst->BlackWins = node->freq[RESULT_Black];
			mvst->Score = node->score / 1000.0;
			mvst->DrawPc = static_cast<double>(node->freq[RESULT_Draw]) / node->total;
			mvst->AvElo = avgElo;
			mvst->Perf = perf;
			mvst->AvYear = static_cast<int>(avgYear);
			mvst->ECO = gcnew System::String(ecoStr);

			mvsts->Add(mvst);
		}

		// Now do the totals line as well, if there are any moves in the tree:

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
			uint nWhite = 0;
			uint nBlack = 0;
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
				nWhite += node->freq[RESULT_White];
				nBlack += node->freq[RESULT_Black];
			}
			totalScore = totalScore * 500 / tree->totalCount;
			int avgElo = 0;
			if (eloCount >= 10)
			{
				avgElo = static_cast<int>(eloSum / eloCount);
			}
			int perf = 0;
			if (perfCount >= 10)
			{
				perf = static_cast<int>(perfSum / perfCount);
			}
			tsts->TotCount = tree->totalCount;
			tsts->TotFreq = 1.0;
			tsts->TotWhiteWins = nWhite;
			tsts->TotDraws = nDraws;
			tsts->TotBlackWins = nBlack;
			tsts->TotScore = totalScore / 1000.0;
			tsts->TotDrawPc = static_cast<double>(nDraws) / tree->totalCount;
			tsts->TotAvElo = avgElo;
			tsts->TotPerf = perf;
			tsts->TotAvYear = yearCount == 0 ? 0 : static_cast<int>((yearSum + (yearCount / 2)) / yearCount);
		}
	}

	return 0;
}

/// <summary>
/// Search: Gets the stats tree for the specified position for the specified database 
/// </summary>
/// <param name="mvsts">the class holding the move line data</param>
/// <param name="tsts">the class holding the total line data</param>
/// <param name="fenstr">the fen of the position</param>
/// <param name="basenum">the number of the base</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Tree::Search(System::Collections::Generic::List<mvstats^>^% mvsts, totstats^% tsts, String^ fenstr, int basenum)
{

	return treeSearch(mvsts, tsts, fenstr, basenum, true);
}

/// <summary>
/// Write: Writes the tree cache file for the specified database
/// </summary>
/// <param name="basenum">the number of the base</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Tree::Write(int basenum)
{
	scidBaseT* base = db;
	if (basenum >= 1 && basenum <= MAX_BASES)
	{
		base = &(dbList[basenum - 1]);
	}
	
	if (!base->inUse)
	{
		return -1;
	}
	if (base->memoryOnly)
	{
		// Memory-only file, so ignore.
		return 0;
	}

	if (base->treeCache->WriteFile(base->fileName) != OK)
	{
		return -2;//Error writing Scid tree cache file.
	}
	return 0;
}

/// <summary>
/// Populate: pulates the treecach form the early moves in the games
/// </summary>
/// <param name="ply">the ply limit on early moves</param>
/// <param name="basenum">the number of the base</param>
/// <param name="numgames">the limit on the number of games to use</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Tree::Populate(int ply, int basenum, uint numgames)
{
	scidBaseT* base = db;
	if (basenum >= 1 && basenum <= MAX_BASES)
	{
		base = &(dbList[basenum - 1]);
	}
	if (!base->inUse)
	{
		return -1;
	}
	if (base->memoryOnly)
	{
		// Memory-only file, so ignore.
		return 0;
	}
	Game* g = new Game;
	// Read each game:
	uint last = db->numGames < numgames ? db->numGames : numgames;
	for (uint i = 1; i <= last; i++)
	{
		if (ScidGame::Load(i)!= 0)
		{
			continue;
		}
		g = new Game(*db->game);
		uint maxPly = ply;
		errorT err = OK;
		System::Collections::Generic::List<mvstats^>^ mvsts = gcnew System::Collections::Generic::List<mvstats^>();
		totstats^ tsts = gcnew totstats();
		do
		{
			//now need to do search
			char boardStr[200];
			g->GetCurrentPos()->PrintFEN(boardStr, FEN_ALL_FIELDS);
			System::String^ fenstr = gcnew System::String(boardStr);
			treeSearch(mvsts, tsts, fenstr, basenum, false);

			err = g->MoveForward();
			maxPly--;
		} while (err == OK && maxPly > 0);

	}
	return 0;
}


// SEARCH functions

/// <summary>
/// Board: Searches for exact match for the current position and sets the filter accordingly.
/// </summary>
/// <param name="fenstr">the position to search</param>
/// <param name="basenum">number of base</param>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Search::Board(String^ fenstr,int basenum)
{
	System::Collections::Generic::List<mvstats^>^ mvsts = gcnew System::Collections::Generic::List<mvstats^> ();
	totstats^ tsts = gcnew totstats();
	return treeSearch(mvsts, tsts, fenstr, basenum, false);
}

// COMPACT functions

void recalcNameFrequencies(NameBase* nb, Index* idx)
{
	for (nameT nt = NAME_FIRST; nt <= NAME_LAST; nt++)
	{
		nb->ZeroAllFrequencies(nt);
	}
	IndexEntry iE;
	for (uint i = 0; i < idx->GetNumGames(); i++)
	{
		idx->ReadEntries(&iE, i, 1);
		nb->IncFrequency(NAME_PLAYER, iE.GetWhite(), 1);
		nb->IncFrequency(NAME_PLAYER, iE.GetBlack(), 1);
		nb->IncFrequency(NAME_EVENT, iE.GetEvent(), 1);
		nb->IncFrequency(NAME_SITE, iE.GetSite(), 1);
		nb->IncFrequency(NAME_ROUND, iE.GetRound(), 1);
	}
}

/// <summary>
/// Games: Compact the game file and index file of a database so all
///    deleted games are removed, the order of game file records
///    matches the index file order, and the game file is the
///    smallest possible size.
/// </summary>
/// <returns>returns 0 if successful</returns>
int ScincFuncs::Compact::Games()
{
	if (db->fileMode == FMODE_ReadOnly)
	{
		return -1;
	}

	// First, create new temporary index and game file:

	fileNameT newName;
	strCopy(newName, db->fileName);
	strAppend(newName, "TEMP");

	Index* newIdx = new Index;
	GFile* newGfile = new GFile;
	// Filter * newFilter = new Filter (0);
	newIdx->SetFileName(newName);

#define CLEANUP                        \
    delete newIdx;                     \
    delete newGfile;                   \
    removeFile(newName, INDEX_SUFFIX); \
    removeFile(newName, GFILE_SUFFIX)
	// ; delete newFilter;

	if (newIdx->CreateIndexFile(FMODE_WriteOnly) != OK)
	{
		CLEANUP;
		return -2;//Error creating temporary file; compaction cancelled
	}
	if (newGfile->Create(newName, FMODE_WriteOnly) != OK)
	{
		CLEANUP;
		return -3;//Error creating temporary file; compaction cancelled
	}

	gameNumberT newNumGames = 0;
	bool interrupted = 0;
	const char* errMsg = "";
	const char* errWrite = "Error writing temporary file; compaction cancelled.";

	for (uint i = 0; i < db->numGames; i++)
	{
		IndexEntry* ieOld = db->idx->FetchEntry(i);
		if (ieOld->GetDeleteFlag())
		{
			continue;
		}
		IndexEntry ieNew;
		errorT err;
		db->bbuf->Empty();
		err = db->gfile->ReadGame(db->bbuf, ieOld->GetOffset(),
			ieOld->GetLength());
		if (err != OK)
		{
			// Just skip corrupt games:
			continue;
		}
		db->bbuf->BackToStart();
		err = scratchGame->Decode(db->bbuf, GAME_DECODE_NONE);
		if (err != OK)
		{
			// Just skip corrupt games:
			continue;
		}
		err = newIdx->AddGame(&newNumGames, &ieNew);
		if (err != OK)
		{
			errMsg = "Error in compaction operation; compaction cencelled.";
			interrupted = true;
			break;
		}
		ieNew = *ieOld;
		db->bbuf->BackToStart();
		uint offset = 0;
		err = newGfile->AddGame(db->bbuf, &offset);
		if (err != OK)
		{
			errMsg = errWrite;
			interrupted = true;
			break;
		}
		ieNew.SetOffset(offset);

		// In 3.2, the maximum value for the game length in half-moves
		// stored in the Index was raised from 255 (8 bits) to over
		// 1000 (10 bits). So if the game and index values do not match,
		// update the index value now:
		if (scratchGame->GetNumHalfMoves() != ieNew.GetNumHalfMoves())
		{
			ieNew.SetNumHalfMoves(scratchGame->GetNumHalfMoves());
		}

		err = newIdx->WriteEntries(&ieNew, newNumGames, 1);
		if (err != OK)
		{
			errMsg = errWrite;
			interrupted = true;
			break;
		}
		// newFilter->Append (db->filter->Get (i));
	}

	if (interrupted)
	{
		CLEANUP;
		return -4;
	}

	newIdx->SetType(db->idx->GetType());
	newIdx->SetDescription(db->idx->GetDescription());

	// Copy custom flags description
	char newDesc[CUSTOM_FLAG_DESC_LENGTH + 1];
	for (byte b = 1; b <= CUSTOM_FLAG_MAX; b++)
	{
		db->idx->GetCustomFlagDesc(newDesc, b);
		newIdx->SetCustomFlagDesc(newDesc, b);
	}

	newIdx->SetAutoLoad(db->idx->GetAutoLoad());
	if (newIdx->CloseIndexFile() != OK || newGfile->Close() != OK)
	{
		CLEANUP;
		errMsg = errWrite;
		return -5;
	}

	// Success: remove old index and game files, and the old filter:

	db->idx->CloseIndexFile();
	db->gfile->Close();
	// These four will fail on windows if a chess engine has been opened after the database
	// due to file descriptor inheritance.  It's a bit hard to handle accurately :(
	removeFile(db->fileName, INDEX_SUFFIX);
	removeFile(db->fileName, GFILE_SUFFIX);
	renameFile(newName, db->fileName, INDEX_SUFFIX);
	renameFile(newName, db->fileName, GFILE_SUFFIX);
	delete newIdx;
	delete newGfile;
	db->idx->SetFileName(db->fileName);
	db->idx->OpenIndexFile(db->fileMode);
	db->idx->ReadEntireFile();
	db->gfile->Open(db->fileName, db->fileMode);

	clearFilter(db, db->idx->GetNumGames());

	db->gameNumber = -1;
	db->numGames = db->idx->GetNumGames();
	recalcNameFrequencies(db->nb, db->idx);
	recalcFlagCounts(db);
	// Remove the out-of-date treecache file:
	db->treeCache->Clear();
	db->backupCache->Clear();
	removeFile(db->fileName, TREEFILE_SUFFIX);

	return 0;
}
