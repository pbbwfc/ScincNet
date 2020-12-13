
#include "ScincFuncs.h"
#include <msclr/marshal.h>

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Global variables:

static scidBaseT* db = NULL;       // current database.
static scidBaseT* clipbase = NULL; // clipbase database.
static scidBaseT* dbList = NULL;   // array of database slots.
static int currentBase = 0;
static Position* scratchPos = NULL;                         // temporary "scratch" position.
static Game* scratchGame = NULL;                            // "scratch" game for searches, etc.


// MAX_BASES is the maximum number of databases that can be open,
// including the clipbase database.
const int MAX_BASES = 9;
const int CLIPBASE_NUM = MAX_BASES - 1;

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
/// <returns>returns 0 if succesful</returns>
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
/// <returns>returns 0 if succesful</returns>
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
/// <returns>returns 0 if succesful</returns>
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
/// <returns>returns 0 if succesful</returns>
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
/// <returns>returns 0 if succesful</returns>
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

// CLIPBASE functions

/// <summary>
/// Clear: Clears the clipbase by closing and recreating it.
/// </summary>
/// <returns>returns 0 if succesful</returns>
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
