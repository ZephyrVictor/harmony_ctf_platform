/**
 * 手工维护的类型定义 — 镜像后端 `src/GZCTF/Services/Import/FlagImportModels.cs`
 *
 * 这些类型在 OpenAPI spec 里不暴露,因为 FlagImportController 用 `[FromForm] string? mapping`
 * 接收 JSON 字符串(而不是 typed body),所以 swagger-typescript-api 生成 Api.ts 时不会产出它们。
 * 前端需要类型化的 mapping 对象,因此在此手动维护。
 *
 * 维护规则:
 *   - 后端 FlagImportModels.cs 改字段时,本文件必须同步更新
 *   - 不要从 @Api 导入这些类型(它们不存在),从此文件导入
 *   - 字段命名与后端 PascalCase → camelCase 转换一致(JSON 序列化默认行为)
 */

import { ChallengeFlagAnswerType } from '@Api'

/** 镜像后端 `FlagImportModels.cs` 的 `ImportMode` enum */
export enum ImportMode {
  Upsert = 'Upsert',
  Skip = 'Skip',
  Fail = 'Fail',
}

/** 镜像后端 `FlagImportModels.cs` 的 `LevelColumnMapping` */
export interface LevelColumnMapping {
  /** @format int32 */
  level: number
  columnName: string
  /** 额外列,与 ColumnName 用 ":" 拼接成最终 flag 值。例: L1 = 文件路径 + ":" + 代码行数 */
  extraColumns?: string[]
  answerType?: ChallengeFlagAnswerType
  /** @format int32 */
  score?: number
  /** @format int32 — 每条 Track×Level 的最大错误提交次数,0 = 用默认值 5 (R4 防爆破) */
  submissionLimit?: number
  /** @format int32 */
  penaltyScore?: number
  lockOnAnswers?: string[]
  banOnAnswers?: string[]
  levelTitle?: string | null
}

/** 镜像后端 `FlagImportModels.cs` 的 `FlagImportColumnMapping` */
export interface FlagImportColumnMapping {
  trackTitleColumn?: string | null
  trackFlawNoColumn?: string | null
  trackRepositoryColumn?: string | null
  trackModuleColumn?: string | null
  trackFilePathColumn?: string | null
  trackCategoryColumn?: string | null
  trackReferenceUrlColumn?: string | null
  /** 子模块列(对应 GameChallenge.Title,自动建题目用) */
  submoduleColumn?: string | null
  /** 仓库列(对应 GameChallenge.Repository,自动建题目用) */
  repositoryColumn?: string | null
  /** FilePath | FlawNo | Title | L1 (推荐 L1：用 L1 flag 值去重，能区分同文件不同行的漏洞) */
  matchBy?: string
  levelColumns?: LevelColumnMapping[]
}
