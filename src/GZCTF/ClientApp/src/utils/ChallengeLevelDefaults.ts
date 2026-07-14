import { ChallengeFlagAnswerType, TrackFullLevelModel } from '@Api'
import { LevelColumnMapping } from '@Utils/ManualApiTypes'

export const COMMIT_TRAP_BAN_ANSWERS = ['0', 'hash(0)', '0000000000000000000000000000000000000000']

export const createDefaultLevelColumnMappings = (): LevelColumnMapping[] => [
  {
    level: 1,
    columnName: '',
    answerType: ChallengeFlagAnswerType.SourceLocation,
    score: 5,
    penaltyScore: 0,
    levelTitle: '漏洞定位',
    caseSensitive: true,
    pathCaseSensitive: true,
    orSeparators: '、,;',
    rangeSeparator: '-',
    stripLinePrefix: true,
    normalizeSourceLocation: true,
  },
  {
    level: 2,
    columnName: '',
    answerType: ChallengeFlagAnswerType.Exact,
    score: 4,
    penaltyScore: 0,
    levelTitle: '漏洞引入',
    caseSensitive: false,
  },
  {
    level: 3,
    columnName: '',
    answerType: ChallengeFlagAnswerType.Exact,
    score: 1,
    penaltyScore: 0,
    levelTitle: '漏洞类型',
    caseSensitive: false,
  },
]

export const createDefaultTrackFullLevels = (): TrackFullLevelModel[] => [
  {
    level: 1,
    flagValue: '',
    answerType: ChallengeFlagAnswerType.SourceLocation,
    score: 5,
    levelTitle: '漏洞定位',
  },
  {
    level: 2,
    flagValue: '',
    answerType: ChallengeFlagAnswerType.Exact,
    score: 4,
    levelTitle: '漏洞引入',
    caseSensitive: false,
  },
  {
    level: 3,
    flagValue: '',
    answerType: ChallengeFlagAnswerType.Exact,
    score: 1,
    levelTitle: '漏洞类型',
    caseSensitive: false,
  },
]
