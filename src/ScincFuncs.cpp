
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

/////////////////////////////////////////////////////////////////////
//  MISC functions
/////////////////////////////////////////////////////////////////////

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

/////////////////////////////////////////////////////////////////////
///  DATABASE functions

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// base_opened:
//    Returns a slot number if the named database is already
//    opened in Scid, or -1 if it is not open.
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

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Autoload:
//   Sets or returns the autoload number of the database, which
//   is the game to load when opening the base.
int ScincFuncs::Base::Autoload(bool getbase, unsigned int basenum)
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

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Open: takes a database name and opens the database.
//    If either the index file or game file cannot be opened for
//    reading and writing, then the database is opened read-only
//    and will not be alterable.
int ScincFuncs::Base::Open(String^ sbasename)
{
    msclr::interop::marshal_context oMarshalContext;

    const char* basename = oMarshalContext.marshal_as<const char*>(sbasename);

    
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

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Close:
//    Closes the current or specified database.
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

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Isreadoonly:
//    is the base read only
bool ScincFuncs::Base::Isreadonly()
{
    return db->inUse && db->fileMode == FMODE_ReadOnly;
}

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// NumGames:
//   Takes optional database number and returns number of games.
int ScincFuncs::Base::NumGames()
{
    scidBaseT* basePtr = db;
    return basePtr->inUse ? basePtr->numGames : 0;
}


//////////////////////////////////////////////////////////////////////
/// CLIPBASE functions

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Clear:
//    Clears the clipbase by closing and recreating it.
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
