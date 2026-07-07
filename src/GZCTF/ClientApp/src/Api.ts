/* eslint-disable */
/* tslint:disable */
// @ts-nocheck
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

export enum ZeroDaySeverity {
  None = "None",
  Low = "Low",
  Medium = "Medium",
  High = "High",
  Critical = "Critical",
}

export enum ZeroDayTicketStatus {
  Pending = "Pending",
  Approved = "Approved",
  Rejected = "Rejected",
}

export enum TicketStatus {
  Pending = "Pending",
  Approved = "Approved",
  Rejected = "Rejected",
}

export enum CaptchaProvider {
  None = "None",
  HashPow = "HashPow",
  CloudflareTurnstile = "CloudflareTurnstile",
}

export enum ContainerPortMappingType {
  Default = "Default",
  PlatformProxy = "PlatformProxy",
}

/** Game event type */
export enum EventType {
  Normal = "Normal",
  ContainerStart = "ContainerStart",
  ContainerDestroy = "ContainerDestroy",
  FlagSubmit = "FlagSubmit",
  CheatDetected = "CheatDetected",
}

/** Submission type */
export enum SubmissionType {
  Unaccepted = "Unaccepted",
  FirstBlood = "FirstBlood",
  SecondBlood = "SecondBlood",
  ThirdBlood = "ThirdBlood",
  Normal = "Normal",
}

export enum BigScreenEventType {
  Bonus = "Bonus",
}

/** Challenge difficulty */
export enum Difficulty {
  Baby = "Baby",
  Trivial = "Trivial",
  Easy = "Easy",
  Normal = "Normal",
  Medium = "Medium",
  Hard = "Hard",
  Expert = "Expert",
  Insane = "Insane",
}

/** Judgement result */
export enum AnswerResult {
  FlagSubmitted = "FlagSubmitted",
  Accepted = "Accepted",
  WrongAnswer = "WrongAnswer",
  CheatDetected = "CheatDetected",
  NotFound = "NotFound",
}

/** Per-flag answer type for hybrid line matching */
export enum ChallengeFlagAnswerType {
  Exact = "Exact",
  SingleLine = "SingleLine",
  LineRange = "LineRange",
  MultiLine = "MultiLine",
  SourceLocation = "SourceLocation",
}

/** Container network mode */
export enum NetworkMode {
  Open = "Open",
  Isolated = "Isolated",
  Custom = "Custom",
}

/** Container status */
export enum ContainerStatus {
  Pending = "Pending",
  Running = "Running",
  Destroyed = "Destroyed",
}

export enum FileType {
  None = "None",
  Local = "Local",
  Remote = "Remote",
}

export enum ChallengeType {
  StaticAttachment = "StaticAttachment",
  StaticContainer = "StaticContainer",
  DynamicAttachment = "DynamicAttachment",
  DynamicContainer = "DynamicContainer",
}

/** Game participant permission */
export enum GamePermission {
  JoinGame = 1,
  RankOverall = 2,
  RequireReview = 4,
  ViewChallenge = 256,
  SubmitFlags = 512,
  GetScore = 1024,
  GetBlood = 2048,
  AffectDynamicScore = 4096,
  All = 2147483647,
}

/** Game announcement type */
export enum NoticeType {
  Normal = "Normal",
  FirstBlood = "FirstBlood",
  SecondBlood = "SecondBlood",
  ThirdBlood = "ThirdBlood",
  NewHint = "NewHint",
  NewChallenge = "NewChallenge",
}

/** Challenge category */
export enum ChallengeCategory {
  Misc = "Misc",
  Crypto = "Crypto",
  Pwn = "Pwn",
  Web = "Web",
  Reverse = "Reverse",
  Blockchain = "Blockchain",
  Forensics = "Forensics",
  Hardware = "Hardware",
  Mobile = "Mobile",
  PPC = "PPC",
  AI = "AI",
  Pentest = "Pentest",
  OSINT = "OSINT",
}

export enum ParticipationStatus {
  Pending = "Pending",
  Accepted = "Accepted",
  Rejected = "Rejected",
  Suspended = "Suspended",
  Unsubmitted = "Unsubmitted",
}

/** Task execution status */
export enum TaskStatus {
  Success = "Success",
  Failed = "Failed",
  Duplicate = "Duplicate",
  Denied = "Denied",
  NotFound = "NotFound",
  Exit = "Exit",
  Unhealthy = "Unhealthy",
  Degraded = "Degraded",
  Pending = "Pending",
}

/** User role enumeration */
export enum Role {
  Banned = "Banned",
  User = "User",
  Monitor = "Monitor",
  Admin = "Admin",
}

/** Login response status */
export enum RegisterStatus {
  LoggedIn = "LoggedIn",
  AdminConfirmationRequired = "AdminConfirmationRequired",
  EmailConfirmationRequired = "EmailConfirmationRequired",
}

/** Request response */
export interface RequestResponseOfRegisterStatus {
  /** Response message */
  title?: string;
  /** Data */
  data?: RegisterStatus;
  /**
   * Status code
   * @format int32
   */
  status?: number;
}

/** Request response */
export interface RequestResponse {
  /** Response message */
  title?: string;
  /**
   * Status code
   * @format int32
   */
  status?: number;
}

/** Account registration */
export type RegisterModel = ModelWithCaptcha & {
  /**
   * Username
   * @minLength 3
   * @maxLength 15
   */
  userName: string;
  /**
   * Password
   * @minLength 1
   */
  password: string;
  /**
   * Email
   * @format email
   * @minLength 1
   */
  email: string;
};

export interface ModelWithCaptcha {
  /** Captcha Challenge */
  challenge?: string | null;
}

/** Account recovery */
export type RecoveryModel = ModelWithCaptcha & {
  /**
   * User email
   * @format email
   * @minLength 1
   */
  email: string;
};

/** Account password reset */
export interface PasswordResetModel {
  /**
   * Password
   * @minLength 1
   */
  password: string;
  /**
   * Email
   * @minLength 1
   */
  email: string;
  /**
   * Base64 formatted token received via email
   * @minLength 1
   */
  rToken: string;
}

/** Account verification */
export interface AccountVerifyModel {
  /**
   * Base64 formatted token received via email
   * @minLength 1
   */
  token: string;
  /**
   * Base64 formatted user email
   * @minLength 1
   */
  email: string;
}

/** Login */
export type LoginModel = ModelWithCaptcha & {
  /**
   * Username or email
   * @minLength 1
   */
  userName: string;
  /**
   * Password
   * @minLength 1
   */
  password: string;
};

/** Basic account information update */
export interface ProfileUpdateModel {
  /**
   * Username
   * @minLength 3
   * @maxLength 15
   */
  userName?: string | null;
  /**
   * Description
   * @maxLength 128
   */
  bio?: string | null;
  /**
   * Phone number
   * @format phone
   */
  phone?: string | null;
  /**
   * Real name
   * @maxLength 128
   */
  realName?: string | null;
  /**
   * Student ID
   * @maxLength 64
   */
  stdNumber?: string | null;
}

/** Password change */
export interface PasswordChangeModel {
  /**
   * Old password
   * @minLength 6
   */
  old: string;
  /**
   * New password
   * @minLength 6
   */
  new: string;
}

/** Request response */
export interface RequestResponseOfBoolean {
  /** Response message */
  title?: string;
  /** Data */
  data?: boolean;
  /**
   * Status code
   * @format int32
   */
  status?: number;
}

/** Email change */
export interface MailChangeModel {
  /**
   * New email
   * @format email
   * @minLength 1
   */
  newMail: string;
}

/** Basic account information */
export interface ProfileUserInfoModel {
  /**
   * User ID
   * @format guid
   */
  userId?: string;
  /** User role */
  role?: Role;
  /** Username */
  userName?: string | null;
  /** Email */
  email?: string | null;
  /** Bio */
  bio?: string | null;
  /** Phone number */
  phone?: string | null;
  /** Real name */
  realName?: string | null;
  /** Student ID */
  stdNumber?: string | null;
  /** Avatar URL */
  avatar?: string | null;
}

/** Global configuration update */
export interface ConfigEditModel {
  /** User policy */
  accountPolicy?: AccountPolicy | null;
  /** Global configuration */
  globalConfig?: GlobalConfig | null;
  /** Game policy */
  containerPolicy?: ContainerPolicy | null;
}

/** Account policy */
export interface AccountPolicy {
  /** Allow user registration */
  allowRegister?: boolean;
  /** Activate account upon registration */
  activeOnRegister?: boolean;
  /** Use captcha verification */
  useCaptcha?: boolean;
  /** Email confirmation required for registration, email change, and password recovery */
  emailConfirmationRequired?: boolean;
  /** Email domain list, separated by commas */
  emailDomainList?: string;
}

/** Global settings */
export interface GlobalConfig {
  /** Platform prefix name */
  title?: string;
  /** Platform slogan */
  slogan?: string;
  /** Site description information */
  description?: string | null;
  /** Footer information */
  footerInfo?: string | null;
  /** Custom theme color */
  customTheme?: string | null;
  /** Use asymmetric encryption for API requests */
  apiEncryption?: boolean;
  /** Platform logo hash */
  logoHash?: string | null;
  /** Platform favicon hash */
  faviconHash?: string | null;
}

/** Container policy */
export interface ContainerPolicy {
  /** Automatically destroy the oldest container when the limit is reached */
  autoDestroyOnLimitReached?: boolean;
  /**
   * User container limit, used to limit the number of exercise containers
   * @format int32
   */
  maxExerciseContainerCountPerUser?: number;
  /**
   * Default container lifetime in minutes
   * @format int32
   * @min 1
   * @max 7200
   */
  defaultLifetime?: number;
  /**
   * Extension duration for each renewal in minutes
   * @format int32
   * @min 1
   * @max 7200
   */
  extensionDuration?: number;
  /**
   * Renewal window before container stops in minutes
   * @format int32
   * @min 1
   * @max 360
   */
  renewalWindow?: number;
}

/** List response */
export interface ArrayResponseOfUserInfoModel {
  /** Data */
  data: UserInfoModel[];
  /**
   * Data length
   * @format int32
   */
  length: number;
  /**
   * Total length
   * @format int32
   */
  total?: number;
}

/** User information (Admin) */
export interface UserInfoModel {
  /**
   * User ID
   * @format guid
   */
  id?: string | null;
  /** Username */
  userName?: string | null;
  /** Real name */
  realName?: string | null;
  /** Student number */
  stdNumber?: string | null;
  /** Contact phone number */
  phone?: string | null;
  /** Bio */
  bio?: string | null;
  /**
   * Registration time
   * @format uint64
   */
  registerTimeUtc?: number;
  /**
   * Last visit time
   * @format uint64
   */
  lastVisitedUtc?: number;
  /** Last visit IP */
  ip?: string;
  /** Email */
  email?: string | null;
  /** Avatar URL */
  avatar?: string | null;
  /** User role */
  role?: Role | null;
  /** Is email confirmed (can log in) */
  emailConfirmed?: boolean | null;
}

/** Batch user creation (Admin) */
export interface UserCreateModel {
  /**
   * Username
   * @minLength 3
   * @maxLength 15
   */
  userName: string;
  /**
   * Password
   * @minLength 1
   */
  password: string;
  /**
   * Email
   * @format email
   * @minLength 1
   */
  email: string;
  /**
   * Real name
   * @maxLength 128
   */
  realName?: string | null;
  /**
   * Student number
   * @maxLength 64
   */
  stdNumber?: string | null;
  /**
   * Contact phone number
   * @format phone
   */
  phone?: string | null;
  /**
   * Team the user joins
   * @maxLength 20
   */
  teamName?: string | null;
}

/** List response */
export interface ArrayResponseOfTeamInfoModel {
  /** Data */
  data: TeamInfoModel[];
  /**
   * Data length
   * @format int32
   */
  length: number;
  /**
   * Total length
   * @format int32
   */
  total?: number;
}

/** Team information */
export interface TeamInfoModel {
  /**
   * Team ID
   * @format int32
   */
  id?: number;
  /** Team name */
  name?: string | null;
  /** Team bio */
  bio?: string | null;
  /** Avatar URL */
  avatar?: string | null;
  /** Is locked */
  locked?: boolean;
  /** Team members */
  members?: TeamUserInfoModel[] | null;
}

/** Team member information */
export interface TeamUserInfoModel {
  /**
   * User ID
   * @format guid
   */
  id?: string | null;
  /** Username */
  userName?: string | null;
  /** Bio */
  bio?: string | null;
  /** Avatar URL */
  avatar?: string | null;
  /** Is Captain */
  captain?: boolean;
}

/** Team information modification (Admin) */
export interface AdminTeamModel {
  /**
   * Team name
   * @maxLength 20
   */
  name?: string | null;
  /**
   * Team bio
   * @maxLength 72
   */
  bio?: string | null;
  /** Is locked */
  locked?: boolean | null;
}

/** User information modification (Admin) */
export interface AdminUserInfoModel {
  /**
   * Username
   * @minLength 3
   * @maxLength 15
   */
  userName?: string | null;
  /**
   * Email
   * @format email
   */
  email?: string | null;
  /**
   * Signature
   * @maxLength 128
   */
  bio?: string | null;
  /**
   * Phone number
   * @format phone
   */
  phone?: string | null;
  /**
   * Real name
   * @maxLength 128
   */
  realName?: string | null;
  /**
   * Student number
   * @maxLength 64
   */
  stdNumber?: string | null;
  /** Is email confirmed (can log in) */
  emailConfirmed?: boolean | null;
  /** User role */
  role?: Role | null;
}

/** Log information (Admin) */
export interface LogMessageModel {
  /**
   * Log time
   * @format uint64
   */
  time?: number;
  /** Username */
  name?: string | null;
  level?: string | null;
  /** IP address */
  ip?: string | null;
  /** Log message */
  msg?: string | null;
  /** Task status */
  status?: TaskStatus | null;
}

/** Modify the participation information */
export interface ParticipationEditModel {
  /** Participation Status */
  status?: ParticipationStatus | null;
  /**
   * The division of the participated team
   * @format int32
   */
  divisionId?: number | null;
}

/** Game writeup information */
export interface WriteupInfoModel {
  /** Division ID to Division Name mapping */
  divisions?: Record<string, string>;
  /** Writeups list */
  writeups?: WriteupInfo[];
}

export interface WriteupInfo {
  /**
   * Participation ID
   * @format int32
   */
  id?: number;
  /** Team information */
  team?: TeamInfoModel;
  /** File URL */
  url?: string;
  /**
   * File upload time
   * @format uint64
   */
  uploadTimeUtc?: number;
  /**
   * The division the team belongs to
   * @format int32
   */
  divisionId?: number | null;
}

/** List response */
export interface ArrayResponseOfContainerInstanceModel {
  /** Data */
  data: ContainerInstanceModel[];
  /**
   * Data length
   * @format int32
   */
  length: number;
  /**
   * Total length
   * @format int32
   */
  total?: number;
}

/** Container instance information (Admin) */
export interface ContainerInstanceModel {
  /** Team */
  team?: TeamModel | null;
  /** Challenge */
  challenge?: ChallengeModel | null;
  /** Container image */
  image?: string;
  /**
   * Container database ID
   * @format guid
   */
  containerGuid?: string;
  /** Container ID */
  containerId?: string;
  /**
   * Container creation time
   * @format uint64
   */
  startedAt?: number;
  /**
   * Expected container stop time
   * @format uint64
   */
  expectStopAt?: number;
  /** Access IP */
  ip?: string;
  /**
   * Access port
   * @format int32
   */
  port?: number;
}

/** Team information */
export interface TeamModel {
  /**
   * Team ID
   * @format int32
   */
  id?: number;
  /** Team name */
  name?: string;
  /** Team avatar */
  avatar?: string | null;
}

/** Challenge information */
export interface ChallengeModel {
  /**
   * Challenge ID
   * @format int32
   */
  id?: number;
  /** Challenge title */
  title?: string;
  /** Challenge category */
  category?: ChallengeCategory;
}

/** List response */
export interface ArrayResponseOfLocalFile {
  /** Data */
  data: LocalFile[];
  /**
   * Data length
   * @format int32
   */
  length: number;
  /**
   * Total length
   * @format int32
   */
  total?: number;
}

export interface LocalFile {
  /**
   * File hash
   * @maxLength 64
   */
  hash?: string;
  /**
   * File name
   * @minLength 1
   */
  name: string;
}

/** This record represents the response for an API token request. */
export interface ApiTokenResponse {
  token?: string;
  /** Represents an API token for programmatic access. */
  info?: ApiToken;
}

/** Represents an API token for programmatic access. */
export interface ApiToken {
  /**
   * The unique identifier for the token, also used as the JWT ID (jti).
   * @format guid
   */
  id?: string;
  /**
   * A user-friendly name for the token to identify its purpose.
   * @minLength 1
   * @maxLength 128
   */
  name: string;
  /**
   * The ID of the user who created the token.
   * @format guid
   * @minLength 1
   */
  creatorId: string;
  /**
   * The timestamp when the token was created.
   * @format uint64
   */
  createdAt: number;
  /**
   * The timestamp when the token expires. A null value means it never expires.
   * @format uint64
   */
  expiresAt?: number | null;
  /**
   * The timestamp when the token was last used.
   * @format uint64
   */
  lastUsedAt?: number | null;
  /** Indicates whether the token has been revoked. */
  isRevoked: boolean;
  /** The name of the user who created the token. */
  creator?: string | null;
}

/** API token creation model. */
export interface ApiTokenCreateModel {
  /**
   * The user-friendly name for the token to identify its purpose.
   * @minLength 1
   * @maxLength 128
   */
  name: string;
  /** The duration for which the token will be valid, in days. */
  expiresIn?: number | null;
}

export interface ProblemDetails {
  type?: string | null;
  title?: string | null;
  /** @format int32 */
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  [key: string]: any;
}

/** Post item (Edit) */
export interface PostEditModel {
  /**
   * Post title
   * @maxLength 50
   */
  title?: string | null;
  /** Post summary */
  summary?: string | null;
  /** Post content */
  content?: string | null;
  /** Post tags */
  tags?: string[] | null;
  /** Is pinned */
  isPinned?: boolean | null;
}

/** Post details */
export interface PostDetailModel {
  /**
   * Post ID
   * @minLength 1
   */
  id: string;
  /**
   * Post title
   * @minLength 1
   */
  title: string;
  /**
   * Post summary
   * @minLength 1
   */
  summary: string;
  /**
   * Post content
   * @minLength 1
   */
  content: string;
  /** Is pinned */
  isPinned: boolean;
  /** Post tags */
  tags?: string[] | null;
  /** Author avatar */
  authorAvatar?: string | null;
  /** Author name */
  authorName?: string | null;
  /**
   * Publish time
   * @format uint64
   */
  time: number;
}

/** Game information (Edit) */
export interface GameInfoModel {
  /**
   * Game ID
   * @format int32
   */
  id?: number;
  /**
   * Game title
   * @minLength 1
   */
  title: string;
  /** Is hidden */
  hidden?: boolean;
  /** Game summary */
  summary?: string;
  /** Game detailed description */
  content?: string;
  /** Accept teams without review */
  acceptWithoutReview?: boolean;
  /** Is writeup required */
  writeupRequired?: boolean;
  /**
   * Game invitation code
   * @maxLength 32
   */
  inviteCode?: string | null;
  /**
   * Team member count limit, 0 means no limit
   * @format int32
   */
  teamMemberCountLimit?: number;
  /**
   * Container count limit per team
   * @format int32
   */
  containerCountLimit?: number;
  /** Game poster URL */
  poster?: string | null;
  /** Game public key */
  publicKey?: string;
  /** Is the game in practice mode (accessible even after the game ends) */
  practiceMode?: boolean;
  /**
   * Start time
   * @format uint64
   */
  start: number;
  /**
   * End time
   * @format uint64
   */
  end: number;
  /**
   * Writeup submission deadline
   * @format uint64
   */
  writeupDeadline?: number;
  /** Writeup additional notes */
  writeupNote?: string;
  /**
   * Blood bonus points
   * @format int64
   */
  bloodBonus?: number;
  /** 0day 提交说明 markdown 文案 */
  zeroDayInfoMarkdown?: string;
  /** 0day 加分基础倍数 */
  zeroDayBaseScore?: number;
  /** 题目冷却分钟数。0 = 不限流 */
  cooldownMinutes?: number;
}

/** List response */
export interface ArrayResponseOfGameInfoModel {
  /** Data */
  data: GameInfoModel[];
  /**
   * Data length
   * @format int32
   */
  length: number;
  /**
   * Total length
   * @format int32
   */
  total?: number;
}

/**
 * Game notice, which will be sent to the client.
 * Information includes first, second, and third blood notifications, hint release notifications, challenging opening notifications, etc.
 */
export type GameNotice = FormattableDataOfNoticeType & {
  /** @format int32 */
  id: number;
  /**
   * Publish time
   * @format uint64
   */
  time: number;
};

/** Formattable data */
export interface FormattableDataOfNoticeType {
  /** Data type */
  type: NoticeType;
  /** List of formatted values */
  values: string[];
}

/** Game notice (Edit) */
export interface GameNoticeModel {
  /**
   * Notice content
   * @minLength 1
   */
  content: string;
}

export interface Division {
  /** @format int32 */
  id: number;
  /**
   * The name of the division.
   * @minLength 1
   * @maxLength 31
   */
  name: string;
  /**
   * Invitation code for joining the division.
   * @maxLength 32
   */
  inviteCode?: string | null;
  /** Permissions associated with the division. */
  defaultPermissions?: GamePermission;
  /** Challenge configs for this division. */
  challengeConfigs?: DivisionChallengeConfig[];
}

export interface DivisionChallengeConfig {
  /** @format int32 */
  challengeId: number;
  /** Challenge Specific Permissions */
  permissions?: GamePermission;
}

export interface DivisionCreateModel {
  /**
   * The name of the division.
   * @minLength 1
   * @maxLength 31
   */
  name: string;
  /**
   * Invitation code for joining the division.
   * @maxLength 32
   */
  inviteCode?: string | null;
  /** Permissions associated with the division. */
  defaultPermissions?: GamePermission | null;
  /** Challenge configs for this division. */
  challengeConfigs?: DivisionChallengeConfigModel[] | null;
}

export interface DivisionChallengeConfigModel {
  /**
   * Challenge ID
   * @format int32
   */
  challengeId: number;
  /** Challenge Specific Permissions */
  permissions?: GamePermission;
}

export interface DivisionEditModel {
  /**
   * The name of the division.
   * @maxLength 31
   */
  name?: string | null;
  /**
   * Invitation code for joining the division.
   * @maxLength 32
   */
  inviteCode?: string | null;
  /** Permissions associated with the division. */
  defaultPermissions?: GamePermission | null;
  /** Challenge configs for this division. */
  challengeConfigs?: DivisionChallengeConfigModel[] | null;
}

/** Challenge detailed information (Edit) */
export interface ChallengeEditDetailModel {
  /**
   * Challenge ID
   * @format int32
   */
  id?: number;
  /**
   * Challenge title
   * @minLength 1
   */
  title: string;
  /** Challenge content */
  content?: string;
  /** Challenge category */
  category: ChallengeCategory;
  /** Challenge type */
  type: ChallengeType;
  /** Challenge hints */
  hints?: string[];
  /**
   * Flag template, used to generate Flag based on Token and challenge, game information
   * @maxLength 120
   */
  flagTemplate?: string | null;
  /** Is the challenge enabled */
  isEnabled: boolean;
  /**
   * Number of people who passed
   * @format int32
   */
  acceptedCount: number;
  /** Unified file name (only for dynamic attachments) */
  fileName?: string | null;
  /** Challenge attachment (dynamic attachments are stored in FlagInfoModel) */
  attachment?: Attachment | null;
  /** Test container */
  testContainer?: ContainerInfoModel | null;
  /** Challenge Flag information */
  flags: FlagInfoModel[];
  /**
   * Image name and tag
   * @minLength 1
   */
  containerImage: string;
  /**
   * Memory limit (MB)
   * @format int32
   */
  memoryLimit?: number | null;
  /**
   * CPU limit (0.1 CPUs)
   * @format int32
   */
  cpuCount?: number | null;
  /**
   * Storage limit (MB)
   * @format int32
   */
  storageLimit?: number | null;
  /**
   * Container exposed port
   * @format int32
   */
  exposePort?: number | null;
  /** Container network mode */
  networkMode?: NetworkMode | null;
  /** Whether to record traffic */
  enableTrafficCapture?: boolean | null;
  /** Whether to disable blood bonus */
  disableBloodBonus?: boolean | null;
  /**
   * The deadline of the challenge, null means no deadline
   * @format uint64
   */
  deadlineUtc?: number | null;
  /**
   * Maximum number of submissions allowed per team (0 = no limit)
   * @format int32
   */
  submissionLimit: number;
  /**
   * 临时全题锁时长（分钟），0 = 永久
   * @format int32
   */
  lockDurationMinutes?: number;
  /**
   * Initial score
   * @format int32
   */
  originalScore: number;
  /**
   * Minimum score rate
   * @format double
   * @min 0
   * @max 1
   */
  minScoreRate: number;
  /**
   * Difficulty coefficient
   * @format double
   */
  difficulty: number;
  /**
   * 代码仓库（作为比赛题目方向分组依据）
   * @maxLength 128
   */
  repository?: string | null;
}

export interface Attachment {
  /** @format int32 */
  id: number;
  /** Attachment type */
  type: FileType;
  /** Default file URL */
  url?: string | null;
  /**
   * Get attachment size
   * @format int64
   */
  fileSize?: number | null;
}

export interface ContainerInfoModel {
  /** Container status */
  status?: ContainerStatus;
  /**
   * Container creation time
   * @format uint64
   */
  startedAt?: number;
  /**
   * Expected container stop time
   * @format uint64
   */
  expectStopAt?: number;
  /** Challenge entry point */
  entry?: string;
}

/** Flag information (Edit) */
export interface FlagInfoModel {
  /**
   * Flag Id
   * @format guid
   */
  id?: string;
  /** Flag text */
  flag?: string;
  /**
   * Level of this flag (1, 2, 3...)
   * @format int32
   */
  level?: number;
  /**
   * Track Id of this flag
   * @format guid
   */
  trackId?: string;
  /** Attachment corresponding to the Flag */
  attachment?: Attachment | null;
}

/** Basic challenge information (Edit) */
export interface ChallengeInfoModel {
  /**
   * Challenge ID
   * @format int32
   */
  id?: number;
  /**
   * Challenge title
   * @minLength 1
   */
  title: string;
  /** Challenge category */
  category?: ChallengeCategory;
  /** Challenge type */
  type?: ChallengeType;
  /** Is the challenge enabled */
  isEnabled?: boolean;
  /**
   * Challenge score
   * @format int32
   */
  score?: number;
  /**
   * Minimum score
   * @format int32
   */
  minScore?: number;
  /**
   * Original score
   * @format int32
   */
  originalScore?: number;
  /**
   * The deadline of the challenge, null means no deadline
   * @format uint64
   */
  deadlineUtc?: number | null;
  /** Repository (for competition direction grouping) */
  repository?: string | null;
}

/** Challenge update information (Edit) */
export interface ChallengeUpdateModel {
  /**
   * Challenge title
   * @minLength 1
   */
  title?: string | null;
  /** Challenge content */
  content?: string | null;
  /**
   * Flag template, used to generate Flag based on Token and challenge/game information
   * @maxLength 120
   */
  flagTemplate?: string | null;
  /** Challenge category */
  category?: ChallengeCategory | null;
  /** Challenge hints */
  hints?: string[] | null;
  /** Is the challenge enabled */
  isEnabled?: boolean | null;
  /** Unified file name */
  fileName?: string | null;
  /**
   * The deadline of the challenge, null means no deadline
   * @format uint64
   */
  deadlineUtc?: number | null;
  /**
   * Maximum number of flag submissions allowed per team for this challenge (0 = no limit)
   * @format int32
   * @min 0
   * @max 10000
   */
  submissionLimit?: number | null;
  /**
   * 临时全题锁时长（分钟），0 = 永久
   * @format int32
   * @min 0
   * @max 2147483647
   */
  lockDurationMinutes?: number | null;
  /** Container image name and tag */
  containerImage?: string | null;
  /**
   * Memory limit (MB)
   * @format int32
   * @min 32
   * @max 1048576
   */
  memoryLimit?: number | null;
  /**
   * CPU limit (0.1 CPUs)
   * @format int32
   * @min 1
   * @max 1024
   */
  cpuCount?: number | null;
  /**
   * Storage limit (MB)
   * @format int32
   * @min 0
   * @max 1048576
   */
  storageLimit?: number | null;
  /**
   * Container exposed port
   * @format int32
   */
  exposePort?: number | null;
  /** Container network mode */
  networkMode?: NetworkMode | null;
  /** Is traffic capture enabled (disabled by default) */
  enableTrafficCapture?: boolean | null;
  /** Is blood bonus disabled (enable by default) */
  disableBloodBonus?: boolean | null;
  /**
   * Initial score
   * @format int32
   */
  originalScore?: number | null;
  /**
   * Minimum score rate
   * @format double
   * @min 0
   * @max 1
   */
  minScoreRate?: number | null;
  /**
   * Difficulty coefficient
   * @format double
   */
  difficulty?: number | null;
  /**
   * 代码仓库（作为比赛题目方向分组依据）
   * @maxLength 128
   */
  repository?: string | null;
}

/** New attachment information (Edit) */
export interface AttachmentCreateModel {
  /** Attachment type */
  attachmentType?: FileType;
  /** File hash (local file) */
  fileHash?: string | null;
  /** File URL (remote file) */
  remoteUrl?: string | null;
}

/** New Flag information (Edit) */
export interface FlagCreateModel {
  /**
   * Flag text
   * @minLength 1
   * @maxLength 512
   */
  flag: string;
  /**
   * Score contribution of this flag
   * @format int32
   */
  score?: number;
  /**
   * Sort order for display
   * @format int32
   */
  sortOrder?: number;
  /** Attachment type */
  attachmentType?: FileType;
  /** File hash (local file) */
  fileHash?: string | null;
  /** File URL (remote file) */
  remoteUrl?: string | null;
}

export interface ChallengeFlag {
  /** @format guid */
  id?: string;
  /**
   * Flag value（匹配用）
   * @minLength 1
   * @maxLength 512
   */
  flagValue: string;
  /** Per-flag 答案类型，决定匹配算法 */
  answerType?: ChallengeFlagAnswerType;
  /**
   * 原始答案文本（归一化前）
   * @maxLength 512
   */
  rawAnswer?: string;
  /**
   * 归一化后的匹配键
   * @maxLength 528
   */
  normalizedAnswerKey?: string;
  /** 是否参与匹配 */
  isEnabled?: boolean;
  /** 是否对选手可见 */
  isVisible?: boolean;
  /** 是否已被分配（dynamic attachment 用） */
  isOccupied?: boolean;
  /**
   * 显示顺序
   * @format int32
   */
  sortOrder?: number;
  /** @format uint64 */
  createdAt?: number;
  /** @format uint64 */
  updatedAt?: number;
  /**
   * 所属层级（1, 2, 3...）
   * @format int32
   */
  level?: number;
  /**
   * 所属 Track（同 TrackId 的 flags 属于同一漏洞实例）
   * @format guid
   */
  trackId?: string;
  /** @format int32 */
  challengeId?: number;
  challenge?: GameChallenge | null;
  /** @format int32 */
  exerciseId?: number | null;
  exercise?: ExerciseChallenge | null;
  /** @format int32 */
  attachmentId?: number | null;
  attachment?: Attachment | null;
  submissionRecords?: SubmissionRecord[];
}

export type GameChallenge = Challenge & {
  /** Whether to record traffic */
  enableTrafficCapture?: boolean;
  /** Whether to disable blood bonus */
  disableBloodBonus?: boolean;
  /**
   * Initial score
   * @format int32
   */
  originalScore: number;
  /**
   * Minimum score rate
   * @format double
   * @min 0
   * @max 1
   */
  minScoreRate: number;
  /**
   * Difficulty coefficient
   * @format double
   */
  difficulty: number;
  /**
   * 代码仓库（作为比赛题目方向分组依据，如 "arkcompiler_ets_runtime"）
   * @maxLength 128
   */
  repository?: string | null;
  /**
   * Current score of the challenge
   * @format int32
   */
  currentScore?: number;
  /** Submissions */
  submissions?: Submission[];
  /** Challenge instances */
  instances?: GameInstance[];
  /** Teams that activated the challenge */
  teams?: Participation[];
  /** Configurations for divisions */
  divisionConfigs?: DivisionChallengeConfig[];
  /** First solves recorded for this challenge. */
  firstSolves?: FirstSolve[] | null;
  /** 多级 flag Track 列表（每个 Track 对应一个漏洞实例） */
  tracks?: ChallengeTrack[];
  /** 多级 flag LevelConfig 列表（所有 Track 共享） */
  levelConfigs?: ChallengeLevelConfig[];
  /**
   * Game ID
   * @format int32
   */
  gameId?: number;
  /** Game object */
  game?: Game;
};

export interface Submission {
  /**
   * Submitted answer string
   * @maxLength 512
   */
  answer?: string;
  /** Status of the submitted answer */
  status?: AnswerResult;
  /**
   * Time the answer was submitted
   * @format uint64
   */
  time?: number;
  /** User who submitted */
  user?: string;
  /** Team that submitted */
  team?: string;
  /** Challenge that was submitted */
  challenge?: string;
}

export type GameInstance = Instance & {
  /** Get instance attachment */
  attachment?: Attachment | null;
  /** Get instance attachment URL */
  attachmentUrl?: string | null;
  /** @format int32 */
  challengeId: number;
  /** Challenge object */
  challenge?: GameChallenge;
  /** @format int32 */
  participationId: number;
  /** Participation team object */
  participation?: Participation;
};

/** Participation information */
export interface Participation {
  /** Participation status */
  status: ParticipationStatus;
  /**
   * Team token
   * @minLength 1
   */
  token: string;
  /** Team writeup */
  writeup?: LocalFile | null;
  /** Members participating in the team */
  members?: UserParticipation[];
  /** Challenges activated by the team */
  challenges?: GameChallenge[];
  /** Game instances */
  instances?: GameInstance[];
  /** Submissions */
  submissions?: Submission[];
  /** First solves recorded for this participation. */
  firstSolves?: FirstSolve[];
  /**
   * Game ID
   * @format int32
   */
  gameId: number;
  /** Game */
  game?: Game;
  /**
   * Team ID
   * @format int32
   */
  teamId: number;
  /** Team */
  team?: Team;
  /**
   * Division ID
   * @format int32
   */
  divisionId?: number | null;
  /** Division this participation belongs to */
  division?: Division | null;
}

export interface UserParticipation {
  /**
   * User ID
   * @format guid
   * @minLength 1
   */
  userId: string;
  /** User */
  user?: UserInfo;
  /**
   * Team ID
   * @format int32
   */
  teamId: number;
  /** Team */
  team?: Team;
  /**
   * Game ID
   * @format int32
   */
  gameId: number;
  /** Game */
  game?: Game;
  /**
   * Participation ID
   * @format int32
   */
  participationId: number;
}

export type UserInfo = IdentityUserOfGuid & {
  /**
   * Override Guid to use Ulid
   * @format guid
   */
  id?: string;
  /** User role */
  role?: Role;
  /** User's recent IP address */
  ip?: string;
  /**
   * User's last sign-in time
   * @format uint64
   */
  lastSignedInUtc?: number;
  /**
   * User's last visit time
   * @format uint64
   */
  lastVisitedUtc?: number;
  /**
   * User registration time
   * @format uint64
   */
  registerTimeUtc?: number;
  /**
   * User bio
   * @maxLength 128
   */
  bio?: string;
  /**
   * Real name
   * @maxLength 128
   */
  realName?: string;
  /**
   * Student ID
   * @maxLength 64
   */
  stdNumber?: string;
  /** Hide in exercise scoreboard */
  exerciseVisible?: boolean;
  avatarUrl?: string | null;
  /**
   * Avatar hash
   * @maxLength 64
   */
  avatarHash?: string | null;
  /** Personal submission records */
  submissions?: Submission[];
  /** Participated teams */
  teams?: Team[];
};

export interface Team {
  /** @format int32 */
  id?: number;
  /**
   * Team name
   * @minLength 1
   * @maxLength 20
   */
  name: string;
  /**
   * Team bio
   * @maxLength 72
   */
  bio?: string | null;
  /**
   * Avatar hash
   * @maxLength 64
   */
  avatarHash?: string | null;
  /** Is the team locked */
  locked?: boolean;
  /**
   * Invite token
   * @maxLength 32
   */
  inviteToken?: string;
  /** Invitation code */
  inviteCode?: string;
  avatarUrl?: string | null;
  /**
   * Captain user ID
   * @format guid
   */
  captainId?: string;
  /** Captain */
  captain?: UserInfo | null;
  /** Participation objects */
  participations?: Participation[];
  /** Games */
  games?: Game[] | null;
  /** Members */
  members?: UserInfo[];
}

export interface Game {
  /** @format int32 */
  id: number;
  /**
   * Game title
   * @minLength 1
   */
  title: string;
  /**
   * Token signature public key
   * @minLength 1
   * @maxLength 63
   */
  publicKey: string;
  /**
   * Token signature private key
   * @minLength 1
   * @maxLength 63
   */
  privateKey: string;
  /** Whether to hide */
  hidden: boolean;
  /** Whether the game is in practice mode (most operations can still be performed after the game ends) */
  practiceMode?: boolean;
  /**
   * Poster hash
   * @maxLength 64
   */
  posterHash?: string | null;
  /** Game description */
  summary?: string;
  /** Detailed introduction of the game */
  content?: string;
  /** Teams can join without review */
  acceptWithoutReview?: boolean;
  /** Whether writeup is required */
  writeupRequired?: boolean;
  /**
   * Game invitation code
   * @maxLength 32
   */
  inviteCode?: string | null;
  /**
   * Limit on the number of team members, 0 means no limit
   * @format int32
   */
  teamMemberCountLimit?: number;
  /**
   * Limit on the number of containers a team can have simultaneously
   * @format int32
   */
  containerCountLimit?: number;
  /**
   * Start time
   * @format uint64
   */
  start: number;
  /**
   * End time
   * @format uint64
   */
  end: number;
  /**
   * Writeup submission deadline
   * @format uint64
   */
  writeupDeadline: number;
  /**
   * Additional notes for writeup
   * @minLength 1
   */
  writeupNote: string;
  /** Blood bonus */
  bloodBonus: BloodBonus;
  /** Poster URL */
  posterUrl?: string | null;
  /** Team hash salt */
  teamHashSalt?: string;
  /** List of divisions for the game */
  divisions?: Division[] | null;
}

/** Blood bonus */
export interface BloodBonus {
  /** @format int64 */
  val?: number;
  /** @format int64 */
  firstBlood?: number;
  /** @format float */
  firstBloodFactor?: number;
  /** @format int64 */
  secondBlood?: number;
  /** @format float */
  secondBloodFactor?: number;
  /** @format int64 */
  thirdBlood?: number;
  /** @format float */
  thirdBloodFactor?: number;
  noBonus?: boolean;
}

export interface IdentityUserOfGuid {
  /** @format guid */
  id?: string;
  userName?: string | null;
  normalizedUserName?: string | null;
  email?: string | null;
  normalizedEmail?: string | null;
  emailConfirmed?: boolean;
  passwordHash?: string | null;
  securityStamp?: string | null;
  concurrencyStamp?: string | null;
  phoneNumber?: string | null;
  phoneNumberConfirmed?: boolean;
  twoFactorEnabled?: boolean;
  /** @format uint64 */
  lockoutEnd?: number | null;
  lockoutEnabled?: boolean;
  /** @format int32 */
  accessFailedCount?: number;
}

/**
 * Represents the first successful solve of a challenge for a specific participation.
 * This table acts as the immutable fact source for scoreboard and scoring related logic.
 */
export interface FirstSolve {
  /**
   * Participation ID.
   * @format int32
   */
  participationId: number;
  /**
   * Challenge ID.
   * @format int32
   */
  challengeId: number;
  /**
   * Submission ID that produced this solve.
   * @format int32
   */
  submissionId: number;
  /** Participation information */
  participation?: Participation;
  challenge?: GameChallenge;
  submission?: Submission;
}

export interface Instance {
  /** Whether the challenge is loaded */
  isLoaded?: boolean;
  /**
   * Last container operation time to ensure operations are not too frequent
   * @format uint64
   */
  lastContainerOperation?: number;
  isContainerOperationTooFrequent?: boolean;
  /** @format guid */
  flagId?: string | null;
  /** Flag context object */
  flagContext?: ChallengeFlag | null;
  /** @format guid */
  containerId?: string | null;
  /** Container object */
  container?: Container | null;
}

export interface Container {
  /**
   * Container GUID
   * @format guid
   */
  id?: string;
  /**
   * The Image used to create the container
   * @minLength 1
   */
  image: string;
  /**
   * Container ID
   * @minLength 1
   */
  containerId: string;
  /** Container status */
  status: ContainerStatus;
  /**
   * Container creation time
   * @format uint64
   */
  startedAt: number;
  /**
   * Expected container stop time
   * @format uint64
   */
  expectStopAt: number;
  /** Whether the container has a reverse proxy */
  isProxy: boolean;
  /**
   * Local IP
   * @minLength 1
   */
  ip: string;
  /**
   * Local port
   * @format int32
   */
  port: number;
  /** Public IP */
  publicIP?: string | null;
  /**
   * Public port
   * @format int32
   */
  publicPort?: number | null;
  /** Container instance access method */
  entry?: string;
  /** Whether traffic capture is enabled */
  enableTrafficCapture?: boolean;
  /** Shortened container GUID for logging purposes */
  shortId?: string;
  /** The container ID for logging purposes */
  logId?: string;
  /** Game challenge instance object */
  gameInstance?: GameInstance | null;
  /**
   * Game challenge instance object ID
   * @format int32
   */
  gameInstanceId?: number | null;
  /** Exercise challenge instance object */
  exerciseInstance?: ExerciseInstance | null;
  /**
   * Exercise challenge instance object ID
   * @format int32
   */
  exerciseInstanceId?: number | null;
}

export type ExerciseInstance = Instance & {
  /** Get instance attachment */
  attachment?: Attachment | null;
  /** Get instance attachment URL */
  attachmentUrl?: string | null;
  /**
   * Time when the answer was solved
   * @format uint64
   */
  solveTimeUtc?: number;
  /**
   * User ID
   * @format guid
   * @minLength 1
   */
  userId: string;
  /** Participating user */
  user?: UserInfo;
  /**
   * Exercise ID
   * @format int32
   */
  exerciseId: number;
  /** Exercise object */
  exercise?: ExerciseChallenge;
};

export type ExerciseChallenge = Challenge & {
  /** Credits for the exercise challenge */
  credit?: boolean;
  /** Difficulty of the exercise challenge, used for tags, sorting, etc. */
  difficulty?: Difficulty;
  /** Additional tags for the exercise challenge */
  tags?: string[] | null;
  /** Dependent exercise challenges */
  dependencies?: ExerciseChallenge[];
};

export interface Challenge {
  /** @format int32 */
  id: number;
  /**
   * Challenge title
   * @minLength 1
   */
  title: string;
  /**
   * Challenge content
   * @minLength 1
   */
  content: string;
  /** Challenge category */
  category: ChallengeCategory;
  /** Challenge type, cannot be changed after creation */
  type: ChallengeType;
  /** Challenge hints */
  hints?: string[] | null;
  /** Whether the challenge is enabled */
  isEnabled?: boolean;
  /**
   * The deadline of the challenge, null means no deadline
   * @format uint64
   */
  deadlineUtc?: number | null;
  /**
   * Maximum number of submissions allowed per team (0 = no limit)
   * @format int32
   */
  submissionLimit: number;
  /**
   * 临时全题锁时长（分钟），0 = 永久
   * @format int32
   * @min 0
   * @max 2147483647
   */
  lockDurationMinutes?: number;
  /** Image name and tag */
  containerImage?: string | null;
  /**
   * Memory limit (MB)
   * @format int32
   */
  memoryLimit?: number | null;
  /**
   * Storage limit (MB)
   * @format int32
   */
  storageLimit?: number | null;
  /**
   * CPU limit (0.1 CPUs)
   * @format int32
   */
  cpuCount?: number | null;
  /**
   * Container exposed port
   * @format int32
   */
  exposePort?: number | null;
  /** Container network mode */
  networkMode?: NetworkMode | null;
  /** Download file name, used only for dynamic attachment unified file name */
  fileName?: string | null;
  /**
   * Flag template, used to generate flags based on token and challenge, game information
   * @maxLength 120
   */
  flagTemplate?: string | null;
  /**
   * Challenge attachment ID
   * @format int32
   */
  attachmentId?: number | null;
  /** Challenge attachment (dynamic attachments are stored per-flag) */
  attachment?: Attachment | null;
  /**
   * Test container ID
   * @format guid
   */
  testContainerId?: string | null;
  /** Test container */
  testContainer?: Container | null;
  /** List of flags for the challenge */
  flags?: ChallengeFlag[];
}

/** 漏洞实例 track（数据集一行 = 一个 Track） */
export interface ChallengeTrack {
  /** @format guid */
  id?: string;
  /** @format int32 */
  challengeId?: number;
  /** @maxLength 200 */
  title?: string | null;
  /** @maxLength 64 */
  flawNo?: string | null;
  /** @maxLength 128 */
  repository?: string | null;
  /** @maxLength 128 */
  module?: string | null;
  /** @maxLength 512 */
  filePath?: string | null;
  /** @maxLength 128 */
  category?: string | null;
  /** @maxLength 512 */
  referenceUrl?: string | null;
  isEnabled?: boolean;
  /** @format int32 */
  sortOrder?: number;
  /** @format uint64 */
  createdAt?: number;
  /** @format uint64 */
  updatedAt?: number;
  challenge?: GameChallenge;
}

/** Per-challenge per-level 配置（所有 Track 共享） */
export interface ChallengeLevelConfig {
  /** @format int32 */
  id?: number;
  /** @format int32 */
  challengeId?: number;
  /**
   * 层级编号，1, 2, 3...
   * @format int32
   */
  level?: number;
  /** @maxLength 64 */
  title?: string | null;
  /**
   * 本 level 解出分值
   * @format int32
   * @min 0
   * @max 2147483647
   */
  score?: number;
  /**
   * 0 = 继承 Challenge.SubmissionLimit
   * @format int32
   * @min 0
   * @max 2147483647
   */
  submissionLimit?: number;
  /**
   * 0 = 继承 ChallengePenalty
   * @format int32
   * @min 0
   * @max 2147483647
   */
  penaltyScore?: number;
  /** 触发临时全题锁的 trap 答案 */
  lockOnAnswers?: string[];
  /** 触发永久 (Track, Level) 封禁的 trap 答案（仅 Level 2+ 生效） */
  banOnAnswers?: string[];
  /** 是否区分答案整体大小写。SourceLocation 下主要用于路径部分。 */
  caseSensitive?: boolean;
  /** SourceLocation 路径部分是否区分大小写。 */
  pathCaseSensitive?: boolean;
  /** SourceLocation 行号 OR 分隔符，例如 "、,"。 */
  orSeparators?: string;
  /** SourceLocation 行号范围分隔符。 */
  rangeSeparator?: string;
  /** 是否剥离行号中的 c: / C: 前缀。 */
  stripLinePrefix?: boolean;
  isEnabled?: boolean;
  /** @format int32 */
  sortOrder?: number;
  challenge?: GameChallenge;
}

export interface SubmissionRecord {
  /** @format guid */
  id?: string;
  /**
   * Score change (positive = correct flag, negative = penalty)
   * @format int32
   */
  scoreChange?: number;
  /** Whether this record represents a penalty (wrong answer) */
  isPenalty?: boolean;
  /** @format uint64 */
  createdAt?: number;
  /**
   * Denormalized game ID
   * @format int32
   */
  gameId?: number | null;
  /**
   * Denormalized challenge ID
   * @format int32
   */
  challengeId?: number | null;
  /**
   * Denormalized participation ID
   * @format int32
   */
  participationId?: number | null;
  /**
   * Denormalized level
   * @format int32
   */
  level?: number | null;
  /**
   * Denormalized track ID
   * @format guid
   */
  trackId?: string | null;
  /**
   * 提交类型：Correct | Wrong | Trap | Ban | Duplicate
   * @maxLength 20
   */
  submissionType?: string | null;
  /** @format int32 */
  submissionId?: number;
  submission?: Submission | null;
  /** @format guid */
  matchedFlagId?: string | null;
  matchedFlag?: ChallengeFlag | null;
}

export interface ChallengeFlagCreateModel {
  flagValue?: string;
  /** Per-flag answer type for hybrid line matching */
  answerType?: ChallengeFlagAnswerType;
  rawAnswer?: string | null;
  isVisible?: boolean;
  isEnabled?: boolean;
  /** @format int32 */
  level?: number;
  /** @format guid */
  trackId?: string | null;
  /** @format int32 */
  sortOrder?: number;
}

export interface ChallengeFlagUpdateModel {
  flagValue?: string | null;
  answerType?: ChallengeFlagAnswerType | null;
  rawAnswer?: string | null;
  isVisible?: boolean | null;
  isEnabled?: boolean | null;
  /** @format int32 */
  level?: number | null;
  /** @format guid */
  trackId?: string | null;
  /** @format int32 */
  sortOrder?: number | null;
}

export interface ChallengePenalty {
  /** @format guid */
  id?: string;
  /**
   * Score deducted for each wrong submission. 0 means no penalty.
   * @format int32
   * @min 0
   * @max 2147483647
   */
  penaltyScore: number;
  /** @format uint64 */
  createdAt?: number;
  /** @format uint64 */
  updatedAt?: number;
  /**
   * Challenge ID
   * @format int32
   */
  challengeId?: number;
  /** Challenge */
  challenge?: GameChallenge | null;
}

export interface PenaltyUpdateModel {
  /** @format int32 */
  penaltyScore?: number;
}

export interface BigScreenEvent {
  /** @format guid */
  id?: string;
  /** Event type */
  eventType?: BigScreenEventType;
  /**
   * Display message
   * @minLength 1
   */
  message: string;
  /**
   * Team name for display
   * @maxLength 50
   */
  teamName?: string | null;
  /**
   * Challenge name for display
   * @maxLength 100
   */
  challengeName?: string | null;
  /**
   * Related score change
   * @format int32
   */
  scoreChange?: number | null;
  /** Whether this event has been consumed by the big screen */
  isConsumed?: boolean;
  /** @format uint64 */
  createdAt?: number;
  /**
   * Challenge ID
   * @format int32
   */
  challengeId?: number;
  /** Challenge */
  challenge?: GameChallenge | null;
  /**
   * Related user ID
   * @format guid
   */
  userId?: string | null;
  /** Related user */
  user?: UserInfo | null;
}

export interface SubmissionRecordDto {
  /** @format guid */
  id?: string;
  /** @format int32 */
  submissionId?: number;
  /** @format int32 */
  scoreChange?: number;
  isPenalty?: boolean;
  /** @format uint64 */
  createdAt?: number;
  challengeName?: string;
  /** @format int32 */
  challengeId?: number;
  /** @format int32 */
  gameId?: number;
  answer?: string | null;
  /** @format guid */
  matchedFlagId?: string | null;
  /** @format int32 */
  level?: number | null;
  /** @format guid */
  trackId?: string | null;
  submissionType?: string | null;
}

export interface FlagImportFilePreview {
  columnNames?: string[];
  sampleRows?: Record<string, string>[];
  /** 试解析出的 Track 预览（需要传入 mapping 才会填充） */
  tracks?: FlagImportTrackPreview[];
  /** 数据集中出现的所有仓库名（供前端显示"将创建 N 个新题目方向"） */
  repositories?: string[];
  /** 数据集中出现的所有子模块名（供前端显示"将创建 N 个新题目"） */
  submodules?: string[];
}

export interface FlagImportTrackPreview {
  title?: string | null;
  flawNo?: string | null;
  repository?: string | null;
  module?: string | null;
  filePath?: string | null;
  category?: string | null;
  referenceUrl?: string | null;
  submodule?: string | null;
  levels?: FlagImportLevelPreview[];
}

export interface FlagImportLevelPreview {
  /** @format int32 */
  level?: number;
  flagValue?: string | null;
  answerType?: string;
  /** @format int32 */
  score?: number;
}

export interface FlagImportResult {
  /** @format int32 */
  totalRows?: number;
  /** @format int32 */
  imported?: number;
  /** @format int32 */
  updated?: number;
  /** @format int32 */
  skipped?: number;
  /** @format int32 */
  failed?: number;
  errors?: FlagImportError[];
  /**
   * 自动建题目模式下，创建的 GameChallenge 数量
   * @format int32
   */
  createdChallenges?: number;
  /**
   * 自动建题目模式下，复用的已存在 GameChallenge 数量
   * @format int32
   */
  reusedChallenges?: number;
}

export interface FlagImportError {
  /** @format int32 */
  row?: number;
  error?: string;
}

/** Basic game information, excluding detailed description and current team registration status */
export interface BasicGameInfoModel {
  /** @format int32 */
  id: number;
  /** Game title */
  title?: string;
  /** Game summary */
  summary?: string;
  /** Poster image URL */
  poster?: string | null;
  /**
   * Team member limit
   * @format int32
   */
  limit?: number;
  /**
   * Start time
   * @format uint64
   */
  start: number;
  /**
   * End time
   * @format uint64
   */
  end: number;
}

/** List response */
export interface ArrayResponseOfBasicGameInfoModel {
  /** Data */
  data: BasicGameInfoModel[];
  /**
   * Data length
   * @format int32
   */
  length: number;
  /**
   * Total length
   * @format int32
   */
  total?: number;
}

/** Detailed game information, including detailed introduction and current team registration status */
export interface DetailedGameInfoModel {
  /** @format int32 */
  id?: number;
  /** Game title */
  title?: string;
  /** Game description */
  summary?: string;
  /** Detailed introduction of the game */
  content?: string;
  /** Whether the game is hidden */
  hidden?: boolean;
  /** List of participation divisions */
  divisions?: DivisionInfo[] | null;
  /** Whether an invitation code is required */
  inviteCodeRequired?: boolean;
  /** Whether writeup submission is required */
  writeupRequired?: boolean;
  /** Game poster URL */
  poster?: string | null;
  /**
   * Team member count limit
   * @format int32
   */
  limit?: number;
  /**
   * Number of teams registered for participation
   * @format int32
   */
  teamCount?: number;
  /**
   * Current registered division
   * @format int32
   */
  division?: number | null;
  /** Team name for participation */
  teamName?: string | null;
  /** Whether the game is in practice mode (can still be accessed after the game ends) */
  practiceMode?: boolean;
  /** Team participation status */
  status?: ParticipationStatus;
  /**
   * Start time
   * @format uint64
   */
  start?: number;
  /**
   * End time
   * @format uint64
   */
  end?: number;
}

export interface DivisionInfo {
  /**
   * Division ID
   * @format int32
   */
  id?: number;
  /** Division name */
  name?: string;
  /** Is the division invite code required */
  inviteCodeRequired?: boolean;
}

export interface GameJoinCheckInfoModel {
  /** The teams that the current user has joined and participated in the game */
  joinedTeams?: JoinedTeam[];
  /** IDs of divisions that can be joined */
  joinableDivisions?: number[];
}

export interface JoinedTeam {
  /**
   * Team ID
   * @format int32
   */
  id: number;
  /**
   * The division ID the team has joined
   * @format int32
   */
  division: number;
}

export interface GameJoinModel {
  /**
   * Team ID for participation
   * @format int32
   */
  teamId: number;
  /**
   * Division for participation
   * @format int32
   */
  divisionId?: number | null;
  /** Invitation code for participation */
  inviteCode?: string | null;
  /** Whether the user has read and accepted the commitment letter */
  commitmentAccepted: boolean;
}

/** Scoreboard */
export interface ScoreboardModel {
  /**
   * Update time
   * @format uint64
   */
  updateTimeUtc: number;
  /**
   * Blood bonus coefficient
   * @format int64
   */
  bloodBonus: number;
  /** List of top ten timelines */
  timelines: TimeLineItem[];
  /** List of team information */
  items: ScoreboardItem[];
  /** List of division information */
  divisions: DivisionItem[];
  /** Challenge information */
  challenges: Record<string, ChallengeInfo[]>;
  /**
   * Number of challenges
   * @format int32
   */
  challengeCount: number;
}

export interface TimeLineItem {
  /** @format int32 */
  divisionId?: number;
  teams?: TopTimeLine[];
}

export interface TopTimeLine {
  /**
   * Team ID
   * @format int32
   */
  id: number;
  /**
   * Team name
   * @minLength 1
   */
  name: string;
  /** Timeline */
  items: TimeLine[];
}

export interface TimeLine {
  /**
   * Time
   * @format uint64
   */
  time: number;
  /**
   * Score
   * @format int32
   */
  score: number;
}

export interface ScoreboardItem {
  /**
   * Team ID
   * @format int32
   */
  id: number;
  /**
   * Team name
   * @minLength 1
   */
  name: string;
  /** Team Bio */
  bio?: string | null;
  /**
   * Division of participation
   * @format int32
   */
  divisionId?: number | null;
  /** Team avatar */
  avatar?: string | null;
  /**
   * Score
   * @format int32
   */
  score: number;
  /**
   * Rank
   * @format int32
   */
  rank: number;
  /**
   * Division rank
   * @format int32
   */
  divisionRank?: number | null;
  /**
   * Last submission time
   * @format uint64
   */
  lastSubmissionTime: number;
  /** List of solved challenges */
  solvedChallenges: ChallengeItem[];
  /**
   * Number of solved (Track, Level) pairs (D8)
   * @format int32
   */
  solvedCount: number;
}

export interface ChallengeItem {
  /**
   * Challenge ID
   * @format int32
   */
  id: number;
  /**
   * Challenge score
   * @format int32
   */
  score: number;
  /** Submission type (unsolved, first blood, second blood, third blood, or others) */
  type: SubmissionType;
  /** Username of the solver */
  userName?: string | null;
  /**
   * Submission time for the challenge, used to calculate the timeline
   * @format uint64
   */
  time: number;
}

export interface DivisionItem {
  /**
   * Division ID
   * @format int32
   */
  id: number;
  /**
   * The name of the division.
   * @minLength 1
   */
  name: string;
  /** Permissions associated with the division. */
  defaultPermissions: GamePermission;
  /** Challenge configs for this division. */
  challengeConfigs: Record<string, DivisionChallengeItem>;
}

export interface DivisionChallengeItem {
  /**
   * Challenge ID
   * @format int32
   */
  challengeId: number;
  /** Permissions for a specific challenge. */
  permissions: GamePermission;
}

export interface ChallengeInfo {
  /**
   * Challenge ID
   * @format int32
   */
  id: number;
  /**
   * Challenge title
   * @minLength 1
   */
  title: string;
  /** Challenge category */
  category: ChallengeCategory;
  /**
   * Challenge score
   * @format int32
   */
  score: number;
  /**
   * Number of teams that solved the challenge
   * @format int32
   */
  solved: number;
  /**
   * The deadline of the challenge, null means no deadline
   * @format uint64
   */
  deadline?: number | null;
  /** Bloods for the challenge */
  bloods: Blood[];
  /** Whether to disable blood bonus */
  disableBloodBonus: boolean;
  /** 代码仓库（作为比赛题目方向分组依据，如 "arkcompiler_ets_runtime"） */
  repository?: string | null;
}

export interface Blood {
  /**
   * Team ID
   * @format int32
   */
  id: number;
  /**
   * Team name
   * @minLength 1
   */
  name: string;
  /** Team avatar */
  avatar?: string | null;
  /**
   * Time when the blood was obtained
   * @format uint64
   */
  submitTimeUtc?: number | null;
}

/**
 * Game event, recorded but not sent to the client.
 * Information includes flag submission, container start/stop, cheating, and score changes.
 */
export type GameEvent = FormattableDataOfEventType & {
  /**
   * Publish time
   * @format uint64
   */
  time: number;
  /** Related username */
  user?: string;
  /** Related team name */
  team?: string;
};

/** Formattable data */
export interface FormattableDataOfEventType {
  /** Data type */
  type: EventType;
  /** List of formatted values */
  values: string[];
}

/** Cheat behavior information */
export interface CheatInfoModel {
  /** Team owning the flag */
  ownedTeam?: ParticipationModel;
  /** Team submitting the flag */
  submitTeam?: ParticipationModel;
  /** Submission corresponding to this cheating behavior */
  submission?: Submission;
}

/** Team participation information */
export interface ParticipationModel {
  /**
   * Participation ID
   * @format int32
   */
  id?: number;
  /** Team information */
  team?: TeamModel;
  /** Team participation status */
  status?: ParticipationStatus;
  /** Team division */
  division?: string | null;
  /**
   * Team division ID
   * @format int32
   */
  divisionId?: number | null;
}

export interface ChallengeTrafficModel {
  /**
   * Challenge ID
   * @format int32
   */
  id?: number;
  /**
   * Challenge title
   * @minLength 1
   */
  title: string;
  /** Challenge category */
  category?: ChallengeCategory;
  /** Challenge type */
  type?: ChallengeType;
  /** Is the challenge enabled */
  isEnabled?: boolean;
  /**
   * Number of team traffic captured by the challenge
   * @format int32
   */
  count?: number;
}

/** Team traffic information */
export interface TeamTrafficModel {
  /**
   * Participation ID
   * @format int32
   */
  id?: number;
  /**
   * Team Id
   * @format int32
   */
  teamId?: number;
  /** Team name */
  name?: string | null;
  /** Division of participation */
  division?: string | null;
  /** Avatar URL */
  avatar?: string | null;
  /**
   * Number of traffic captured by the challenge
   * @format int32
   */
  count?: number;
}

/** File record */
export interface FileRecord {
  /** File name */
  fileName?: string;
  /**
   * File size
   * @format int64
   */
  size?: number;
  /**
   * File modification date
   * @format uint64
   */
  updateTime?: number;
}

export interface GameDetailModel {
  /** Challenge information */
  challenges?: Record<string, ChallengeInfo[]>;
  /**
   * Number of challenges
   * @format int32
   */
  challengeCount?: number;
  /** Scoreboard information */
  rank?: ScoreboardItem | null;
  /**
   * Team token
   * @minLength 1
   */
  teamToken: string;
  /** Whether writeup submission is required */
  writeupRequired: boolean;
  /**
   * Writeup submission deadline
   * @format uint64
   */
  writeupDeadline: number;
  /** 0day 提交说明 markdown 文案 */
  zeroDayInfoMarkdown?: string;
  /** 0day 加分基础倍数 */
  zeroDayBaseScore?: number;
  /** 题目冷却分钟数。0 = 不限流 */
  cooldownMinutes?: number;
}

/** Participation for review (Admin) */
export interface ParticipationInfoModel {
  /**
   * Participation ID
   * @format int32
   */
  id: number;
  /** Participating team */
  team: TeamWithDetailedUserInfo;
  /** Registered members */
  registeredMembers: string[];
  /**
   * Division of the game
   * @format int32
   */
  divisionId?: number | null;
  /** Participation status */
  status: ParticipationStatus;
}

/** Detailed team information for review (Admin) */
export interface TeamWithDetailedUserInfo {
  /**
   * Team ID
   * @format int32
   */
  id?: number;
  /** Is locked */
  locked?: boolean;
  /**
   * Captain ID
   * @format guid
   */
  captainId?: string;
  /** Team name */
  name?: string | null;
  /** Team bio */
  bio?: string | null;
  /** Avatar URL */
  avatar?: string | null;
  /** Team members */
  members?: ProfileUserInfoModel[];
}

/** Challenge detailed information */
export interface ChallengeDetailModel {
  /**
   * Challenge ID
   * @format int32
   */
  id?: number;
  /** Challenge title */
  title?: string;
  /** Challenge content */
  content?: string;
  /** Challenge category */
  category?: ChallengeCategory;
  /** Challenge hints */
  hints?: string[] | null;
  /**
   * Current score of the challenge
   * @format int32
   */
  score?: number;
  /** Challenge type */
  type?: ChallengeType;
  /** Flag context */
  context?: ClientFlagContext;
  /**
   * Maximum number of attempts allowed (0 = no limit)
   * @format int32
   */
  limit?: number;
  /**
   * 临时全题锁时长（分钟），0 = 永久（只读，展示用）
   * @format int32
   */
  lockDurationMinutes?: number;
  /** Player-visible flag metadata (legacy HybridLine view; superseded by Tracks) */
  flags?: HybridLineFlagInfoModel[];
  /** Whether all enabled flags have been solved */
  allEnabledFlagsSolved?: boolean;
  /**
   * Current attempt count
   * @format int32
   */
  attempts?: number;
  /**
   * Deadline of the challenge, null means no deadline
   * @format uint64
   */
  deadline?: number | null;
  /** 多级 flag 状态：每个 Track 的进度 */
  tracks?: TrackStatusModel[] | null;
  /** 全题锁状态 */
  challengeLock?: ChallengeLockStatusModel | null;
  /**
   * 当前可提交的最低 Level（前端引导用）
   * @format int32
   */
  currentLevel?: number;
  /** 代码仓库（作为比赛题目方向分组依据，如 "arkcompiler_ets_runtime"） */
  repository?: string | null;
  /**
   * 当前活跃 TrackId（最近一次有提交记录的 Track）
   * @format guid
   */
  activeTrackId?: string | null;
  /** 题目冷却信息（per-challenge 答错次数 + 临时冷却） */
  cooldown?: CooldownInfo | null;
}

export interface ClientFlagContext {
  /**
   * Close time of the challenge instance
   * @format uint64
   */
  closeTime?: number | null;
  /** Connection method of the challenge instance */
  instanceEntry?: string | null;
  /** Attachment URL */
  url?: string | null;
  /**
   * Attachment file size
   * @format int64
   */
  fileSize?: number | null;
}

export interface HybridLineFlagInfoModel {
  /** @format guid */
  id?: string;
  /** @format int32 */
  level?: number;
  /** @format guid */
  trackId?: string;
  /** @format int32 */
  sortOrder?: number;
  /** Per-flag answer type for hybrid line matching */
  answerType?: ChallengeFlagAnswerType;
  isSolved?: boolean;
}

/** 选手视图的 Track 状态 */
export interface TrackStatusModel {
  /** @format guid */
  trackId?: string;
  title?: string | null;
  isEnabled?: boolean;
  levels?: TrackLevelStatusModel[];
}

/** 选手视图的 Track × Level 状态 */
export interface TrackLevelStatusModel {
  /** @format int32 */
  level?: number;
  title?: string | null;
  /** Unsolved | Solved | Banned | Locked */
  status?: string;
  /** @format uint64 */
  solvedAt?: number | null;
  /** @format int32 */
  attemptsUsed?: number;
  /** @format uint64 */
  unlocksAt?: number | null;
}

/** 题目冷却状态快照（返回给前端） */
export interface CooldownInfo {
  /** 当前周期错答次数 */
  /** @format int32 */
  wrongCount?: number;
  /** 错答次数阈值（= trackCount × 5）。0 = 不限流 */
  /** @format int32 */
  wrongAnswerLimit?: number;
  /** 冷却分钟数（per-game 配置） */
  /** @format int32 */
  cooldownMinutes?: number;
  /** 冷却到期时间。null = 未冷却；> now = 冷却中 */
  /** @format uint64 */
  cooldownUntil?: number | null;
}

/** 全题锁状态（选手视图） */
export interface ChallengeLockStatusModel {
  isLocked?: boolean;
  /** @format uint64 */
  unlocksAt?: number | null;
  reason?: string | null;
  triggerAnswer?: string | null;
  /** @format guid */
  bannedTrackId?: string | null;
  /** @format int32 */
  bannedLevel?: number | null;
  banTriggerAnswer?: string | null;
}

/** Flag submission response (D6) */
export interface FlagSubmitResponse {
  /**
   * Submission ID
   * @format int32
   */
  id: number;
  /** Result message (e.g., "Accepted", "Wrong answer", "Trap answer triggered") */
  message?: string | null;
  /**
   * Matched Track ID (D6: L1 response returns matched trackId for L2+ submissions)
   * @format guid
   */
  trackId?: string | null;
  /**
   * Matched Level (D6: current level that was submitted)
   * @format int32
   */
  level?: number | null;
  /**
   * 当前题目下已解漏洞数（按 Track 计数，L1+L2+L3 全解才算 1 个）
   * @format int32
   */
  solvedCount?: number | null;
  /**
   * 当前活跃 TrackId（= TrackId 字段的显式别名，供前端刷新后恢复）
   * @format guid
   */
  activeTrackId?: string | null;
  /** 该 Track 的最高 Level 是否已解（提示选手"该漏洞已完整解决"） */
  isTrackComplete?: boolean | null;
  /** 是否触发临时全题锁 */
  challengeLocked?: boolean;
  /**
   * 全题锁解锁时间
   * @format uint64
   */
  unlocksAt?: number | null;
  /**
   * 触发封禁的 TrackId
   * @format guid
   */
  bannedTrackId?: string | null;
  /**
   * 触发封禁的 Level
   * @format int32
   */
  bannedLevel?: number | null;
  /** 触发锁/封禁的错误答案（用于详细提示） */
  triggerAnswer?: string | null;
  /** 题目冷却信息（per-challenge 答错次数 + 临时冷却） */
  cooldown?: CooldownInfo | null;
}

/** Flag submission */
export interface FlagSubmitModel {
  /**
   * Flag content
   * @minLength 1
   */
  flag: string;
  /**
   * Track Id (Level 1 入口可为空；Level ≥2 必填)
   * @format guid
   */
  trackId?: string | null;
  /**
   * Level (1, 2, 3...)
   * @format int32
   */
  level?: number;
}

/** Game writeup submission information */
export interface BasicWriteupInfoModel {
  /** Whether it has been submitted */
  submitted?: boolean;
  /** File name */
  name?: string;
  /**
   * File size
   * @format int64
   */
  fileSize?: number;
  /** Writeup additional notes */
  note?: string;
}

/** Post information */
export interface PostInfoModel {
  /**
   * Post ID
   * @minLength 1
   */
  id: string;
  /**
   * Post title
   * @minLength 1
   */
  title: string;
  /**
   * Post summary
   * @minLength 1
   */
  summary: string;
  /** Is pinned */
  isPinned: boolean;
  /** Post tags */
  tags?: string[] | null;
  /** Author avatar */
  authorAvatar?: string | null;
  /** Author name */
  authorName?: string | null;
  /**
   * Update time
   * @format uint64
   */
  time: number;
}

/** Client configuration */
export interface ClientConfig {
  /** Platform prefix name */
  title?: string;
  /** Platform slogan */
  slogan?: string;
  /** Footer information */
  footerInfo?: string | null;
  /** Custom theme color */
  customTheme?: string | null;
  /** The public key used for API requests */
  apiPublicKey?: string | null;
  /** Platform logo URL */
  logoUrl?: string | null;
  /** Container port mapping type */
  portMapping?: ContainerPortMappingType;
  /**
   * Default container lifetime in minutes
   * @format int32
   */
  defaultLifetime?: number;
  /**
   * Extension duration for each renewal in minutes
   * @format int32
   */
  extensionDuration?: number;
  /**
   * Renewal window before container stops in minutes
   * @format int32
   */
  renewalWindow?: number;
}

/** Client CAPTCHA information */
export interface ClientCaptchaInfoModel {
  /** Captcha Provider Type */
  type?: CaptchaProvider;
  /** Site Key */
  siteKey?: string;
}

/** Hash Pow verification */
export interface HashPowChallenge {
  /** Challenge ID */
  id?: string;
  /** Verification challenge */
  challenge?: string;
  /**
   * Difficulty coefficient
   * @format int32
   */
  difficulty?: number;
}

export interface LevelConfigCreateModel {
  /**
   * @format int32
   * @min 1
   * @max 2147483647
   */
  level?: number;
  /** @maxLength 64 */
  title?: string | null;
  /**
   * @format int32
   * @min 0
   * @max 2147483647
   */
  score?: number;
  /**
   * @format int32
   * @min 0
   * @max 2147483647
   */
  submissionLimit?: number | null;
  /**
   * @format int32
   * @min 0
   * @max 2147483647
   */
  penaltyScore?: number | null;
  lockOnAnswers?: string[] | null;
  banOnAnswers?: string[] | null;
  caseSensitive?: boolean | null;
  pathCaseSensitive?: boolean | null;
  /** @maxLength 32 */
  orSeparators?: string | null;
  /** @maxLength 8 */
  rangeSeparator?: string | null;
  stripLinePrefix?: boolean | null;
  isEnabled?: boolean | null;
  /**
   * @format int32
   * @min 0
   * @max 2147483647
   */
  sortOrder?: number | null;
}

export interface LevelConfigUpdateModel {
  /** @maxLength 64 */
  title?: string | null;
  /**
   * @format int32
   * @min 0
   * @max 2147483647
   */
  score?: number | null;
  /**
   * @format int32
   * @min 0
   * @max 2147483647
   */
  submissionLimit?: number | null;
  /**
   * @format int32
   * @min 0
   * @max 2147483647
   */
  penaltyScore?: number | null;
  lockOnAnswers?: string[] | null;
  banOnAnswers?: string[] | null;
  caseSensitive?: boolean | null;
  pathCaseSensitive?: boolean | null;
  /** @maxLength 32 */
  orSeparators?: string | null;
  /** @maxLength 8 */
  rangeSeparator?: string | null;
  stripLinePrefix?: boolean | null;
  isEnabled?: boolean | null;
  /**
   * @format int32
   * @min 0
   * @max 2147483647
   */
  sortOrder?: number | null;
}

/** Team information update */
export interface TeamUpdateModel {
  /**
   * Team name
   * @maxLength 20
   */
  name?: string | null;
  /**
   * Team bio
   * @maxLength 72
   */
  bio?: string | null;
}

export interface TeamTransferModel {
  /**
   * New captain ID
   * @format guid
   * @minLength 1
   */
  newCaptainId: string;
}

/** Signature verification */
export interface SignatureVerifyModel {
  /**
   * Team token
   * @minLength 1
   */
  teamToken: string;
  /**
   * Game public key, Base64 encoded
   * @minLength 1
   */
  publicKey: string;
}

export interface TicketDto {
  /** @format guid */
  id?: string;
  description?: string;
  status?: TicketStatus;
  reviewComment?: string | null;
  /** @format int32 */
  refundScore?: number;
  /** @format int32 */
  bonusScore?: number;
  /** @format uint64 */
  createdAt?: number;
  /** @format uint64 */
  reviewedAt?: number | null;
  /** @format int32 */
  submissionId?: number;
  /** @format int32 */
  gameId?: number | null;
  userName?: string | null;
  reviewerName?: string | null;
  challengeName?: string | null;
  answer?: string | null;
  /** @format int32 */
  penaltyScore?: number;
  attachments?: TicketAttachmentDto[];
}

export interface TicketAttachmentDto {
  /** @format guid */
  id?: string;
  fileName?: string;
  /** @format int64 */
  fileSize?: number;
  url?: string | null;
  contentType?: string | null;
  /** @format uint64 */
  createdAt?: number;
}

export interface TicketCreateModel {
  /** @format int32 */
  submissionId?: number;
  /** @format int32 */
  gameId?: number | null;
  description?: string;
}

export interface TicketReviewModel {
  /** @format int32 */
  refundScore?: number;
  /** @format int32 */
  bonusScore?: number;
  comment?: string | null;
}

export interface TrackCreateModel {
  /** @maxLength 200 */
  title?: string | null;
  /** @maxLength 64 */
  flawNo?: string | null;
  /** @maxLength 128 */
  repository?: string | null;
  /** @maxLength 128 */
  module?: string | null;
  /** @maxLength 512 */
  filePath?: string | null;
  /** @maxLength 128 */
  category?: string | null;
  /** @maxLength 512 */
  referenceUrl?: string | null;
  isEnabled?: boolean | null;
  /**
   * @format int32
   * @min 0
   * @max 2147483647
   */
  sortOrder?: number | null;
}

export interface TrackFullCreateResult {
  /** 漏洞实例 track（数据集一行 = 一个 Track） */
  track?: ChallengeTrack;
  flags?: ChallengeFlag[];
}

export interface TrackFullCreateModel {
  track?: TrackCreateModel | null;
  levels?: TrackFullLevelModel[];
}

export interface TrackFullLevelModel {
  /**
   * @format int32
   * @min 1
   * @max 2147483647
   */
  level?: number;
  /**
   * @minLength 1
   * @maxLength 512
   */
  flagValue: string;
  /** Per-flag answer type for hybrid line matching */
  answerType?: ChallengeFlagAnswerType;
  /**
   * 该级别分值（默认 L1=5 / L2=4 / L3=1，其余 = 0）
   * @format int32
   */
  score?: number;
  /** @format int32 */
  penaltyScore?: number;
  lockOnAnswers?: string[];
  banOnAnswers?: string[];
  levelTitle?: string | null;
  caseSensitive?: boolean;
  pathCaseSensitive?: boolean;
  orSeparators?: string | null;
  rangeSeparator?: string | null;
  stripLinePrefix?: boolean;
}

export interface TrackUpdateModel {
  /** @maxLength 200 */
  title?: string | null;
  /** @maxLength 64 */
  flawNo?: string | null;
  /** @maxLength 128 */
  repository?: string | null;
  /** @maxLength 128 */
  module?: string | null;
  /** @maxLength 512 */
  filePath?: string | null;
  /** @maxLength 128 */
  category?: string | null;
  /** @maxLength 512 */
  referenceUrl?: string | null;
  isEnabled?: boolean | null;
  /**
   * @format int32
   * @min 0
   * @max 2147483647
   */
  sortOrder?: number | null;
}

export interface ZeroDayTicketDto {
  /** @format guid */
  id?: string;
  title?: string | null;
  description?: string;
  status?: ZeroDayTicketStatus;
  reviewComment?: string | null;
  /** @format int32 */
  bonusScore?: number;
  /** @format uint64 */
  createdAt?: number;
  /** @format uint64 */
  reviewedAt?: number | null;
  /** @format int32 */
  gameId?: number;
  /** @format int32 */
  challengeId?: number | null;
  userName?: string | null;
  reviewerName?: string | null;
  gameTitle?: string | null;
  challengeTitle?: string | null;
  severity?: ZeroDaySeverity;
  module?: string | null;
  filePath?: string | null;
  lineNumber?: string | null;
  commitHash?: string | null;
  cwe?: string | null;
  poC?: string | null;
  /** @format guid */
  createdTrackId?: string | null;
}

export interface ZeroDayTicketCreateModel {
  title?: string | null;
  description?: string;
  /** @format int32 */
  gameId?: number;
  /** @format int32 */
  challengeId?: number | null;
  /** 严重度（Critical/High/Medium/Low/None）— 决定加分梯度参考 */
  severity?: ZeroDaySeverity;
  /** 模块/子模块路径 */
  module?: string | null;
  /** 文件路径（L1 主键） */
  filePath?: string | null;
  /** 行号或范围（"c:1060", "c:550-559", "c:281、c:286"） */
  lineNumber?: string | null;
  /** 修复 commit hash */
  commitHash?: string | null;
  /** CWE（多个用 "; " 分隔） */
  cwe?: string | null;
  /** PoC / 佐证 */
  poC?: string | null;
}

export interface ZeroDayTicketReviewModel {
  /** @format int32 */
  bonusScore?: number;
  comment?: string | null;
  /** Admin-selected ChallengeId (used when ticket has no ChallengeId, so bonus can be awarded) */
  challengeId?: number | null;
}

import { apiLanguage } from "@Utils/I18n";
import type {
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  HeadersDefaults,
  ResponseType,
} from "axios";
import axios from "axios";

export type QueryParamsType = Record<string | number, any>;

export interface FullRequestParams
  extends Omit<AxiosRequestConfig, "data" | "params" | "url" | "responseType"> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean;
  /** request path */
  path: string;
  /** content type of request body */
  type?: ContentType;
  /** query params */
  query?: QueryParamsType;
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseType;
  /** request body */
  body?: unknown;
}

export type RequestParams = Omit<
  FullRequestParams,
  "body" | "method" | "query" | "path"
>;

export interface ApiConfig<SecurityDataType = unknown>
  extends Omit<AxiosRequestConfig, "data" | "cancelToken"> {
  securityWorker?: (
    securityData: SecurityDataType | null,
  ) => Promise<AxiosRequestConfig | void> | AxiosRequestConfig | void;
  secure?: boolean;
  format?: ResponseType;
}

export enum ContentType {
  Json = "application/json",
  FormData = "multipart/form-data",
  UrlEncoded = "application/x-www-form-urlencoded",
  Text = "text/plain",
}

export class HttpClient<SecurityDataType = unknown> {
  public instance: AxiosInstance;
  private securityData: SecurityDataType | null = null;
  private securityWorker?: ApiConfig<SecurityDataType>["securityWorker"];
  private secure?: boolean;
  private format?: ResponseType;

  constructor({
    securityWorker,
    secure,
    format,
    ...axiosConfig
  }: ApiConfig<SecurityDataType> = {}) {
    this.instance = axios.create({
      ...axiosConfig,
      baseURL: axiosConfig.baseURL || "",
    });
    this.secure = secure;
    this.format = format;
    this.securityWorker = securityWorker;
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data;
  };

  protected mergeRequestParams(
    params1: AxiosRequestConfig,
    params2?: AxiosRequestConfig,
  ): AxiosRequestConfig {
    const method = params1.method || (params2 && params2.method);

    return {
      ...this.instance.defaults,
      ...params1,
      ...params2,
      headers: {
        ...(method &&
          this.instance.defaults.headers[
            method.toLowerCase() as keyof HeadersDefaults
          ]),
        ...params1.headers,
        ...(params2 && params2.headers),
      },
    };
  }

  protected stringifyFormItem(formItem: unknown) {
    if (typeof formItem === "object" && formItem !== null) {
      return JSON.stringify(formItem);
    } else {
      return `${formItem}`;
    }
  }

  protected createFormData(input: Record<string, unknown>): FormData {
    return Object.keys(input || {}).reduce((formData, key) => {
      const property = input[key];
      const propertyContent: any[] =
        property instanceof Array ? property : [property];

      for (const formItem of propertyContent) {
        const isFileType = formItem instanceof Blob || formItem instanceof File;
        formData.append(
          key,
          isFileType ? formItem : this.stringifyFormItem(formItem),
        );
      }

      return formData;
    }, new FormData());
  }

  public request = async <T = any, _E = any>({
    secure,
    path,
    type,
    query,
    format,
    body,
    ...params
  }: FullRequestParams): Promise<AxiosResponse<T>> => {
    const secureParams =
      ((typeof secure === "boolean" ? secure : this.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {};
    const requestParams = this.mergeRequestParams(params, secureParams);
    const responseFormat = format || this.format || undefined;

    if (
      type === ContentType.FormData &&
      body &&
      body !== null &&
      typeof body === "object"
    ) {
      body = this.createFormData(body as Record<string, unknown>);
    }

    if (
      type === ContentType.Text &&
      body &&
      body !== null &&
      typeof body !== "string"
    ) {
      body = JSON.stringify(body);
    }

    return this.instance.request({
      ...requestParams,
      headers: {
        ...requestParams.headers,
        ...(type && type !== ContentType.FormData
          ? { "Content-Type": type }
          : {}),
        "Accept-Language": apiLanguage,
      },
      params: query,
      responseType: responseFormat,
      data: body,
      url: path,
    });
  };
}

import useSWR, { MutatorOptions, SWRConfiguration, mutate } from "swr";

/**
 * @title GZCTF Server API
 * @version v1
 *
 * GZCTF Server API Document
 */
export class Api<
  SecurityDataType extends unknown,
> extends HttpClient<SecurityDataType> {
  account = {
    /**
     * @description Use this API to update user's avatar. User permissions required.
     *
     * @tags Account
     * @name AccountAvatar
     * @summary Update user avatar
     * @request PUT:/api/account/avatar
     */
    accountAvatar: (
      data: {
        /** @format binary */
        file?: File | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<string, RequestResponse>({
        path: `/api/account/avatar`,
        method: "PUT",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to change user's email. User permissions required. Email URL: /confirm
     *
     * @tags Account
     * @name AccountChangeEmail
     * @summary User email change
     * @request PUT:/api/account/changeemail
     */
    accountChangeEmail: (data: MailChangeModel, params: RequestParams = {}) =>
      this.request<RequestResponseOfBoolean, RequestResponse>({
        path: `/api/account/changeemail`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to change user's password. User permissions required.
     *
     * @tags Account
     * @name AccountChangePassword
     * @summary User password change
     * @request PUT:/api/account/changepassword
     */
    accountChangePassword: (
      data: PasswordChangeModel,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/account/changepassword`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to log in to the account.
     *
     * @tags Account
     * @name AccountLogIn
     * @summary User login
     * @request POST:/api/account/login
     */
    accountLogIn: (data: LoginModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/login`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to log out of the account. User permissions required.
     *
     * @tags Account
     * @name AccountLogOut
     * @summary User logout
     * @request POST:/api/account/logout
     */
    accountLogOut: (params: RequestParams = {}) =>
      this.request<void, any>({
        path: `/api/account/logout`,
        method: "POST",
        ...params,
      }),

    /**
     * @description Use this API to confirm email change. Email verification code required. User permissions required.
     *
     * @tags Account
     * @name AccountMailChangeConfirm
     * @summary User email change confirmation
     * @request POST:/api/account/mailchangeconfirm
     */
    accountMailChangeConfirm: (
      data: AccountVerifyModel,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/account/mailchangeconfirm`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to reset the password. Email verification code is required.
     *
     * @tags Account
     * @name AccountPasswordReset
     * @summary User password reset
     * @request POST:/api/account/passwordreset
     */
    accountPasswordReset: (
      data: PasswordResetModel,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/account/passwordreset`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to get user information. User permissions required.
     *
     * @tags Account
     * @name AccountProfile
     * @summary Get user information
     * @request GET:/api/account/profile
     */
    accountProfile: (params: RequestParams = {}) =>
      this.request<ProfileUserInfoModel, RequestResponse>({
        path: `/api/account/profile`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get user information. User permissions required.
     *
     * @tags Account
     * @name AccountProfile
     * @summary Get user information
     * @request GET:/api/account/profile
     */
    useAccountProfile: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ProfileUserInfoModel, RequestResponse>(
        doFetch ? `/api/account/profile` : null,
        options,
      ),

    /**
     * @description Use this API to get user information. User permissions required.
     *
     * @tags Account
     * @name AccountProfile
     * @summary Get user information
     * @request GET:/api/account/profile
     */
    mutateAccountProfile: (
      data?: ProfileUserInfoModel | Promise<ProfileUserInfoModel>,
      options?: MutatorOptions,
    ) => mutate<ProfileUserInfoModel>(`/api/account/profile`, data, options),

    /**
     * @description Use this API to request password recovery. Sends an email to the user. Email URL: /reset
     *
     * @tags Account
     * @name AccountRecovery
     * @summary User password recovery request
     * @request POST:/api/account/recovery
     */
    accountRecovery: (data: RecoveryModel, params: RequestParams = {}) =>
      this.request<RequestResponse, RequestResponse>({
        path: `/api/account/recovery`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to register a new user. In development environment, no verification. Email URL: /verify
     *
     * @tags Account
     * @name AccountRegister
     * @summary User registration
     * @request POST:/api/account/register
     */
    accountRegister: (data: RegisterModel, params: RequestParams = {}) =>
      this.request<RequestResponseOfRegisterStatus, RequestResponse>({
        path: `/api/account/register`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to update username and description. User permissions required.
     *
     * @tags Account
     * @name AccountUpdate
     * @summary User data update
     * @request PUT:/api/account/update
     */
    accountUpdate: (data: ProfileUpdateModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/update`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to confirm email using the verification code.
     *
     * @tags Account
     * @name AccountVerify
     * @summary User email confirmation
     * @request POST:/api/account/verify
     */
    accountVerify: (data: AccountVerifyModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/verify`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),
  };
  admin = {
    /**
     * @description Use this API to add users in batch, requires Admin permission
     *
     * @tags Admin
     * @name AdminAddUsers
     * @summary Add users in batch
     * @request POST:/api/admin/users
     */
    adminAddUsers: (data: UserCreateModel[], params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/users`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to delete team, requires Admin permission
     *
     * @tags Admin
     * @name AdminDeleteTeam
     * @summary Delete team
     * @request DELETE:/api/admin/teams/{id}
     */
    adminDeleteTeam: (id: number, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/admin/teams/${id}`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to delete user, requires Admin permission
     *
     * @tags Admin
     * @name AdminDeleteUser
     * @summary Delete user
     * @request DELETE:/api/admin/users/{userid}
     */
    adminDeleteUser: (userid: string, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/admin/users/${userid}`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to forcibly delete container instance, requires Admin permission
     *
     * @tags Admin
     * @name AdminDestroyInstance
     * @summary Delete container instance
     * @request DELETE:/api/admin/instances/{id}
     */
    adminDestroyInstance: (id: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/instances/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Use this API to download all Writeups, requires Admin permission
     *
     * @tags Admin
     * @name AdminDownloadAllWriteups
     * @summary Download all Writeups
     * @request GET:/api/admin/writeups/{id}/all
     */
    adminDownloadAllWriteups: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/writeups/${id}/all`,
        method: "GET",
        ...params,
      }),

    /**
     * @description Use this API to get all files, requires Admin permission
     *
     * @tags Admin
     * @name AdminFiles
     * @summary Get all files
     * @request GET:/api/admin/files
     */
    adminFiles: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 50
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfLocalFile, RequestResponse>({
        path: `/api/admin/files`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get all files, requires Admin permission
     *
     * @tags Admin
     * @name AdminFiles
     * @summary Get all files
     * @request GET:/api/admin/files
     */
    useAdminFiles: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 50
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ArrayResponseOfLocalFile, RequestResponse>(
        doFetch ? [`/api/admin/files`, query] : null,
        options,
      ),

    /**
     * @description Use this API to get all files, requires Admin permission
     *
     * @tags Admin
     * @name AdminFiles
     * @summary Get all files
     * @request GET:/api/admin/files
     */
    mutateAdminFiles: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 50
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: ArrayResponseOfLocalFile | Promise<ArrayResponseOfLocalFile>,
      options?: MutatorOptions,
    ) =>
      mutate<ArrayResponseOfLocalFile>(
        [`/api/admin/files`, query],
        data,
        options,
      ),

    /**
     * @description Use this API to get global settings, requires Admin permission
     *
     * @tags Admin
     * @name AdminGetConfigs
     * @summary Get configuration
     * @request GET:/api/admin/config
     */
    adminGetConfigs: (params: RequestParams = {}) =>
      this.request<ConfigEditModel, RequestResponse>({
        path: `/api/admin/config`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get global settings, requires Admin permission
     *
     * @tags Admin
     * @name AdminGetConfigs
     * @summary Get configuration
     * @request GET:/api/admin/config
     */
    useAdminGetConfigs: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ConfigEditModel, RequestResponse>(
        doFetch ? `/api/admin/config` : null,
        options,
      ),

    /**
     * @description Use this API to get global settings, requires Admin permission
     *
     * @tags Admin
     * @name AdminGetConfigs
     * @summary Get configuration
     * @request GET:/api/admin/config
     */
    mutateAdminGetConfigs: (
      data?: ConfigEditModel | Promise<ConfigEditModel>,
      options?: MutatorOptions,
    ) => mutate<ConfigEditModel>(`/api/admin/config`, data, options),

    /**
     * @description Use this API to get all container instances, requires Admin permission
     *
     * @tags Admin
     * @name AdminInstances
     * @summary Get all container instances
     * @request GET:/api/admin/instances
     */
    adminInstances: (params: RequestParams = {}) =>
      this.request<ArrayResponseOfContainerInstanceModel, RequestResponse>({
        path: `/api/admin/instances`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get all container instances, requires Admin permission
     *
     * @tags Admin
     * @name AdminInstances
     * @summary Get all container instances
     * @request GET:/api/admin/instances
     */
    useAdminInstances: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ArrayResponseOfContainerInstanceModel, RequestResponse>(
        doFetch ? `/api/admin/instances` : null,
        options,
      ),

    /**
     * @description Use this API to get all container instances, requires Admin permission
     *
     * @tags Admin
     * @name AdminInstances
     * @summary Get all container instances
     * @request GET:/api/admin/instances
     */
    mutateAdminInstances: (
      data?:
        | ArrayResponseOfContainerInstanceModel
        | Promise<ArrayResponseOfContainerInstanceModel>,
      options?: MutatorOptions,
    ) =>
      mutate<ArrayResponseOfContainerInstanceModel>(
        `/api/admin/instances`,
        data,
        options,
      ),

    /**
     * @description Use this API to get all logs, requires Admin permission
     *
     * @tags Admin
     * @name AdminLogs
     * @summary Get all logs
     * @request GET:/api/admin/logs
     */
    adminLogs: (
      query?: {
        /** @default "All" */
        level?: string | null;
        /**
         * @format int32
         * @min 0
         * @max 1000
         * @default 50
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<LogMessageModel[], RequestResponse>({
        path: `/api/admin/logs`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get all logs, requires Admin permission
     *
     * @tags Admin
     * @name AdminLogs
     * @summary Get all logs
     * @request GET:/api/admin/logs
     */
    useAdminLogs: (
      query?: {
        /** @default "All" */
        level?: string | null;
        /**
         * @format int32
         * @min 0
         * @max 1000
         * @default 50
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<LogMessageModel[], RequestResponse>(
        doFetch ? [`/api/admin/logs`, query] : null,
        options,
      ),

    /**
     * @description Use this API to get all logs, requires Admin permission
     *
     * @tags Admin
     * @name AdminLogs
     * @summary Get all logs
     * @request GET:/api/admin/logs
     */
    mutateAdminLogs: (
      query?: {
        /** @default "All" */
        level?: string | null;
        /**
         * @format int32
         * @min 0
         * @max 1000
         * @default 50
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: LogMessageModel[] | Promise<LogMessageModel[]>,
      options?: MutatorOptions,
    ) => mutate<LogMessageModel[]>([`/api/admin/logs`, query], data, options),

    /**
     * @description Use this API to update team participation status, review application, requires Admin permission
     *
     * @tags Admin
     * @name AdminParticipation
     * @summary Update participation status
     * @request PUT:/api/admin/participation/{id}
     */
    adminParticipation: (
      id: number,
      data: ParticipationEditModel,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/participation/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to reset the platform Logo, requires Admin permission
     *
     * @tags Admin
     * @name AdminResetLogo
     * @summary Reset platform Logo
     * @request DELETE:/api/admin/config/logo
     */
    adminResetLogo: (params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/config/logo`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Use this API to reset user password, requires Admin permission
     *
     * @tags Admin
     * @name AdminResetPassword
     * @summary Reset user password
     * @request DELETE:/api/admin/users/{userid}/password
     */
    adminResetPassword: (userid: string, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/admin/users/${userid}/password`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to search teams, requires Admin permission
     *
     * @tags Admin
     * @name AdminSearchTeams
     * @summary Search teams
     * @request POST:/api/admin/teams/search
     */
    adminSearchTeams: (
      query?: {
        hint?: string;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfTeamInfoModel, RequestResponse>({
        path: `/api/admin/teams/search`,
        method: "POST",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to search users, requires Admin permission
     *
     * @tags Admin
     * @name AdminSearchUsers
     * @summary Search users
     * @request POST:/api/admin/users/search
     */
    adminSearchUsers: (
      query?: {
        hint?: string;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfUserInfoModel, RequestResponse>({
        path: `/api/admin/users/search`,
        method: "POST",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to get all teams, requires Admin permission
     *
     * @tags Admin
     * @name AdminTeams
     * @summary Get all team information
     * @request GET:/api/admin/teams
     */
    adminTeams: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfTeamInfoModel, RequestResponse>({
        path: `/api/admin/teams`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get all teams, requires Admin permission
     *
     * @tags Admin
     * @name AdminTeams
     * @summary Get all team information
     * @request GET:/api/admin/teams
     */
    useAdminTeams: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ArrayResponseOfTeamInfoModel, RequestResponse>(
        doFetch ? [`/api/admin/teams`, query] : null,
        options,
      ),

    /**
     * @description Use this API to get all teams, requires Admin permission
     *
     * @tags Admin
     * @name AdminTeams
     * @summary Get all team information
     * @request GET:/api/admin/teams
     */
    mutateAdminTeams: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?:
        | ArrayResponseOfTeamInfoModel
        | Promise<ArrayResponseOfTeamInfoModel>,
      options?: MutatorOptions,
    ) =>
      mutate<ArrayResponseOfTeamInfoModel>(
        [`/api/admin/teams`, query],
        data,
        options,
      ),

    /**
     * @description Use this API to change global settings, requires Admin permission
     *
     * @tags Admin
     * @name AdminUpdateConfigs
     * @summary Change configuration
     * @request PUT:/api/admin/config
     */
    adminUpdateConfigs: (data: ConfigEditModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/config`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to change the platform Logo, requires Admin permission
     *
     * @tags Admin
     * @name AdminUpdateLogo
     * @summary Change platform Logo
     * @request POST:/api/admin/config/logo
     */
    adminUpdateLogo: (
      data: {
        /** @format binary */
        file?: File | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/config/logo`,
        method: "POST",
        body: data,
        type: ContentType.FormData,
        ...params,
      }),

    /**
     * @description Use this API to modify team information, requires Admin permission
     *
     * @tags Admin
     * @name AdminUpdateTeam
     * @summary Modify team information
     * @request PUT:/api/admin/teams/{id}
     */
    adminUpdateTeam: (
      id: number,
      data: AdminTeamModel,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/teams/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to modify user information, requires Admin permission
     *
     * @tags Admin
     * @name AdminUpdateUserInfo
     * @summary Modify user information
     * @request PUT:/api/admin/users/{userid}
     */
    adminUpdateUserInfo: (
      userid: string,
      data: AdminUserInfoModel,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/users/${userid}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to get user information, requires Admin permission
     *
     * @tags Admin
     * @name AdminUserInfo
     * @summary Get user information
     * @request GET:/api/admin/users/{userid}
     */
    adminUserInfo: (userid: string, params: RequestParams = {}) =>
      this.request<ProfileUserInfoModel, RequestResponse>({
        path: `/api/admin/users/${userid}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get user information, requires Admin permission
     *
     * @tags Admin
     * @name AdminUserInfo
     * @summary Get user information
     * @request GET:/api/admin/users/{userid}
     */
    useAdminUserInfo: (
      userid: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ProfileUserInfoModel, RequestResponse>(
        doFetch ? `/api/admin/users/${userid}` : null,
        options,
      ),

    /**
     * @description Use this API to get user information, requires Admin permission
     *
     * @tags Admin
     * @name AdminUserInfo
     * @summary Get user information
     * @request GET:/api/admin/users/{userid}
     */
    mutateAdminUserInfo: (
      userid: string,
      data?: ProfileUserInfoModel | Promise<ProfileUserInfoModel>,
      options?: MutatorOptions,
    ) =>
      mutate<ProfileUserInfoModel>(`/api/admin/users/${userid}`, data, options),

    /**
     * @description Use this API to get all users, requires Admin permission
     *
     * @tags Admin
     * @name AdminUsers
     * @summary Get all users
     * @request GET:/api/admin/users
     */
    adminUsers: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfUserInfoModel, RequestResponse>({
        path: `/api/admin/users`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get all users, requires Admin permission
     *
     * @tags Admin
     * @name AdminUsers
     * @summary Get all users
     * @request GET:/api/admin/users
     */
    useAdminUsers: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ArrayResponseOfUserInfoModel, RequestResponse>(
        doFetch ? [`/api/admin/users`, query] : null,
        options,
      ),

    /**
     * @description Use this API to get all users, requires Admin permission
     *
     * @tags Admin
     * @name AdminUsers
     * @summary Get all users
     * @request GET:/api/admin/users
     */
    mutateAdminUsers: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?:
        | ArrayResponseOfUserInfoModel
        | Promise<ArrayResponseOfUserInfoModel>,
      options?: MutatorOptions,
    ) =>
      mutate<ArrayResponseOfUserInfoModel>(
        [`/api/admin/users`, query],
        data,
        options,
      ),

    /**
     * @description Use this API to get Writeup basic information, requires Admin permission
     *
     * @tags Admin
     * @name AdminWriteups
     * @summary Get all Writeup basic information
     * @request GET:/api/admin/writeups/{id}
     */
    adminWriteups: (id: number, params: RequestParams = {}) =>
      this.request<WriteupInfoModel, RequestResponse>({
        path: `/api/admin/writeups/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get Writeup basic information, requires Admin permission
     *
     * @tags Admin
     * @name AdminWriteups
     * @summary Get all Writeup basic information
     * @request GET:/api/admin/writeups/{id}
     */
    useAdminWriteups: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<WriteupInfoModel, RequestResponse>(
        doFetch ? `/api/admin/writeups/${id}` : null,
        options,
      ),

    /**
     * @description Use this API to get Writeup basic information, requires Admin permission
     *
     * @tags Admin
     * @name AdminWriteups
     * @summary Get all Writeup basic information
     * @request GET:/api/admin/writeups/{id}
     */
    mutateAdminWriteups: (
      id: number,
      data?: WriteupInfoModel | Promise<WriteupInfoModel>,
      options?: MutatorOptions,
    ) => mutate<WriteupInfoModel>(`/api/admin/writeups/${id}`, data, options),
  };
  apiToken = {
    /**
     * No description
     *
     * @tags ApiToken
     * @name ApiTokenGenerateToken
     * @summary Generates a new API token.
     * @request POST:/api/tokens
     */
    apiTokenGenerateToken: (
      data: ApiTokenCreateModel,
      params: RequestParams = {},
    ) =>
      this.request<ApiTokenResponse, RequestResponse>({
        path: `/api/tokens`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags ApiToken
     * @name ApiTokenListTokens
     * @summary Lists all API tokens.
     * @request GET:/api/tokens
     */
    apiTokenListTokens: (params: RequestParams = {}) =>
      this.request<ApiToken[], RequestResponse>({
        path: `/api/tokens`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags ApiToken
     * @name ApiTokenListTokens
     * @summary Lists all API tokens.
     * @request GET:/api/tokens
     */
    useApiTokenListTokens: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ApiToken[], RequestResponse>(
        doFetch ? `/api/tokens` : null,
        options,
      ),

    /**
     * No description
     *
     * @tags ApiToken
     * @name ApiTokenListTokens
     * @summary Lists all API tokens.
     * @request GET:/api/tokens
     */
    mutateApiTokenListTokens: (
      data?: ApiToken[] | Promise<ApiToken[]>,
      options?: MutatorOptions,
    ) => mutate<ApiToken[]>(`/api/tokens`, data, options),

    /**
     * No description
     *
     * @tags ApiToken
     * @name ApiTokenRestoreToken
     * @summary Restores an API token.
     * @request POST:/api/tokens/{id}/restore
     */
    apiTokenRestoreToken: (id: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse | ProblemDetails>({
        path: `/api/tokens/${id}/restore`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags ApiToken
     * @name ApiTokenRevokeToken
     * @summary Revokes an API token.
     * @request DELETE:/api/tokens/{id}
     */
    apiTokenRevokeToken: (
      id: string,
      query?: {
        /**
         * If true, the token will be deleted instead of just revoked.
         * @default false
         */
        delete?: boolean;
      },
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse | ProblemDetails>({
        path: `/api/tokens/${id}`,
        method: "DELETE",
        query: query,
        ...params,
      }),
  };
  assets = {
    /**
     * @description Delete a file by hash
     *
     * @tags Assets
     * @name AssetsDelete
     * @summary File deletion interface
     * @request DELETE:/api/assets/{hash}
     */
    assetsDelete: (hash: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse | ProblemDetails>({
        path: `/api/assets/${hash}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Retrieve a file by hash, filename is not matched
     *
     * @tags Assets
     * @name AssetsGetFile
     * @summary File retrieval interface
     * @request GET:/assets/{hash}/{filename}
     */
    assetsGetFile: (
      hash: string,
      filename: string,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/assets/${hash}/${filename}`,
        method: "GET",
        ...params,
      }),

    /**
     * @description Upload one or more files
     *
     * @tags Assets
     * @name AssetsUpload
     * @summary File upload interface
     * @request POST:/api/assets
     */
    assetsUpload: (
      data: {
        files?: File[] | null;
      },
      query?: {
        /** Unified filename */
        filename?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<LocalFile[], RequestResponse>({
        path: `/api/assets`,
        method: "POST",
        query: query,
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),
  };
  edit = {
    /**
     * @description Adding a game challenge flag requires administrator privileges
     *
     * @tags Edit
     * @name EditAddFlags
     * @summary Add Game Challenge Flag
     * @request POST:/api/edit/games/{id}/challenges/{cId}/flags
     */
    editAddFlags: (
      id: number,
      cId: number,
      data: FlagCreateModel[],
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/flags`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Adding a game requires administrator privileges
     *
     * @tags Edit
     * @name EditAddGame
     * @summary Add Game
     * @request POST:/api/edit/games
     */
    editAddGame: (data: GameInfoModel, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Adding a game challenge requires administrator privileges
     *
     * @tags Edit
     * @name EditAddGameChallenge
     * @summary Add Game Challenge
     * @request POST:/api/edit/games/{id}/challenges
     */
    editAddGameChallenge: (
      id: number,
      data: ChallengeInfoModel,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeEditDetailModel, RequestResponse>({
        path: `/api/edit/games/${id}/challenges`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Adding a game notice requires administrator privileges
     *
     * @tags Edit
     * @name EditAddGameNotice
     * @summary Add Game Notice
     * @request POST:/api/edit/games/{id}/notices
     */
    editAddGameNotice: (
      id: number,
      data: GameNoticeModel,
      params: RequestParams = {},
    ) =>
      this.request<GameNotice, RequestResponse>({
        path: `/api/edit/games/${id}/notices`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Adding a post requires administrator privileges
     *
     * @tags Edit
     * @name EditAddPost
     * @summary Add Post
     * @request POST:/api/edit/posts
     */
    editAddPost: (data: PostEditModel, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/edit/posts`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Add a new division for a game; requires administrator privileges
     *
     * @tags Edit
     * @name EditCreateDivision
     * @summary Create Division
     * @request POST:/api/edit/games/{id}/divisions
     */
    editCreateDivision: (
      id: number,
      data: DivisionCreateModel,
      params: RequestParams = {},
    ) =>
      this.request<Division, RequestResponse>({
        path: `/api/edit/games/${id}/divisions`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Testing a game challenge container requires administrator privileges
     *
     * @tags Edit
     * @name EditCreateTestContainer
     * @summary Test Game Challenge Container
     * @request POST:/api/edit/games/{id}/challenges/{cId}/container
     */
    editCreateTestContainer: (
      id: number,
      cId: number,
      params: RequestParams = {},
    ) =>
      this.request<ContainerInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/container`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * @description Delete a division for a game; requires administrator privileges
     *
     * @tags Edit
     * @name EditDeleteDivision
     * @summary Delete Division
     * @request DELETE:/api/edit/games/{id}/divisions/{divisionId}
     */
    editDeleteDivision: (
      id: number,
      divisionId: number,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/divisions/${divisionId}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Deleting a game requires administrator privileges
     *
     * @tags Edit
     * @name EditDeleteGame
     * @summary Delete Game
     * @request DELETE:/api/edit/games/{id}
     */
    editDeleteGame: (id: number, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Deleting a game notice requires administrator privileges
     *
     * @tags Edit
     * @name EditDeleteGameNotice
     * @summary Delete Game Notice
     * @request DELETE:/api/edit/games/{id}/notices/{noticeId}
     */
    editDeleteGameNotice: (
      id: number,
      noticeId: number,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/notices/${noticeId}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Deleting all WriteUps for a game requires administrator privileges
     *
     * @tags Edit
     * @name EditDeleteGameWriteUps
     * @summary Delete All WriteUps
     * @request DELETE:/api/edit/games/{id}/writeups
     */
    editDeleteGameWriteUps: (id: number, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}/writeups`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Deleting a post requires administrator privileges
     *
     * @tags Edit
     * @name EditDeletePost
     * @summary Delete Post
     * @request DELETE:/api/edit/posts/{id}
     */
    editDeletePost: (id: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/posts/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Destroying a test game challenge container requires administrator privileges
     *
     * @tags Edit
     * @name EditDestroyTestContainer
     * @summary Destroy Test Game Challenge Container
     * @request DELETE:/api/edit/games/{id}/challenges/{cId}/container
     */
    editDestroyTestContainer: (
      id: number,
      cId: number,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/container`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Export game with all challenges, divisions, and attachments as a ZIP file; requires Admin permission
     *
     * @tags Edit
     * @name EditExportGame
     * @summary Export game package
     * @request POST:/api/edit/games/{id}/export
     */
    editExportGame: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/export`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Edit
     * @name EditFlushScoreboardCache
     * @summary Flush Scoreboard Cache
     * @request POST:/api/edit/games/{id}/scoreboard/flush
     */
    editFlushScoreboardCache: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/scoreboard/flush`,
        method: "POST",
        ...params,
      }),

    /**
     * @description Retrieve all divisions for a game; requires administrator privileges
     *
     * @tags Edit
     * @name EditGetDivisions
     * @summary Get Divisions
     * @request GET:/api/edit/games/{id}/divisions
     */
    editGetDivisions: (id: number, params: RequestParams = {}) =>
      this.request<Division[], RequestResponse>({
        path: `/api/edit/games/${id}/divisions`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieve all divisions for a game; requires administrator privileges
     *
     * @tags Edit
     * @name EditGetDivisions
     * @summary Get Divisions
     * @request GET:/api/edit/games/{id}/divisions
     */
    useEditGetDivisions: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<Division[], RequestResponse>(
        doFetch ? `/api/edit/games/${id}/divisions` : null,
        options,
      ),

    /**
     * @description Retrieve all divisions for a game; requires administrator privileges
     *
     * @tags Edit
     * @name EditGetDivisions
     * @summary Get Divisions
     * @request GET:/api/edit/games/{id}/divisions
     */
    mutateEditGetDivisions: (
      id: number,
      data?: Division[] | Promise<Division[]>,
      options?: MutatorOptions,
    ) => mutate<Division[]>(`/api/edit/games/${id}/divisions`, data, options),

    /**
     * @description Retrieving a game requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGame
     * @summary Get Game
     * @request GET:/api/edit/games/{id}
     */
    editGetGame: (id: number, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieving a game requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGame
     * @summary Get Game
     * @request GET:/api/edit/games/{id}
     */
    useEditGetGame: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<GameInfoModel, RequestResponse>(
        doFetch ? `/api/edit/games/${id}` : null,
        options,
      ),

    /**
     * @description Retrieving a game requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGame
     * @summary Get Game
     * @request GET:/api/edit/games/{id}
     */
    mutateEditGetGame: (
      id: number,
      data?: GameInfoModel | Promise<GameInfoModel>,
      options?: MutatorOptions,
    ) => mutate<GameInfoModel>(`/api/edit/games/${id}`, data, options),

    /**
     * @description Retrieving a game challenge requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameChallenge
     * @summary Get Game Challenge
     * @request GET:/api/edit/games/{id}/challenges/{cId}
     */
    editGetGameChallenge: (
      id: number,
      cId: number,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeEditDetailModel, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieving a game challenge requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameChallenge
     * @summary Get Game Challenge
     * @request GET:/api/edit/games/{id}/challenges/{cId}
     */
    useEditGetGameChallenge: (
      id: number,
      cId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeEditDetailModel, RequestResponse>(
        doFetch ? `/api/edit/games/${id}/challenges/${cId}` : null,
        options,
      ),

    /**
     * @description Retrieving a game challenge requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameChallenge
     * @summary Get Game Challenge
     * @request GET:/api/edit/games/{id}/challenges/{cId}
     */
    mutateEditGetGameChallenge: (
      id: number,
      cId: number,
      data?: ChallengeEditDetailModel | Promise<ChallengeEditDetailModel>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeEditDetailModel>(
        `/api/edit/games/${id}/challenges/${cId}`,
        data,
        options,
      ),

    /**
     * @description Retrieving all game challenges requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameChallenges
     * @summary Get All Game Challenges
     * @request GET:/api/edit/games/{id}/challenges
     */
    editGetGameChallenges: (id: number, params: RequestParams = {}) =>
      this.request<ChallengeInfoModel[], RequestResponse>({
        path: `/api/edit/games/${id}/challenges`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieving all game challenges requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameChallenges
     * @summary Get All Game Challenges
     * @request GET:/api/edit/games/{id}/challenges
     */
    useEditGetGameChallenges: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeInfoModel[], RequestResponse>(
        doFetch ? `/api/edit/games/${id}/challenges` : null,
        options,
      ),

    /**
     * @description Retrieving all game challenges requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameChallenges
     * @summary Get All Game Challenges
     * @request GET:/api/edit/games/{id}/challenges
     */
    mutateEditGetGameChallenges: (
      id: number,
      data?: ChallengeInfoModel[] | Promise<ChallengeInfoModel[]>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeInfoModel[]>(
        `/api/edit/games/${id}/challenges`,
        data,
        options,
      ),

    /**
     * @description Retrieving game notices requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameNotices
     * @summary Get Game Notices
     * @request GET:/api/edit/games/{id}/notices
     */
    editGetGameNotices: (id: number, params: RequestParams = {}) =>
      this.request<GameNotice[], RequestResponse>({
        path: `/api/edit/games/${id}/notices`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieving game notices requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameNotices
     * @summary Get Game Notices
     * @request GET:/api/edit/games/{id}/notices
     */
    useEditGetGameNotices: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<GameNotice[], RequestResponse>(
        doFetch ? `/api/edit/games/${id}/notices` : null,
        options,
      ),

    /**
     * @description Retrieving game notices requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameNotices
     * @summary Get Game Notices
     * @request GET:/api/edit/games/{id}/notices
     */
    mutateEditGetGameNotices: (
      id: number,
      data?: GameNotice[] | Promise<GameNotice[]>,
      options?: MutatorOptions,
    ) => mutate<GameNotice[]>(`/api/edit/games/${id}/notices`, data, options),

    /**
     * @description Retrieving the game list requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGames
     * @summary Get Game List
     * @request GET:/api/edit/games
     */
    editGetGames: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 100
         */
        count?: number;
        /** @format int32 */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfGameInfoModel, RequestResponse>({
        path: `/api/edit/games`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieving the game list requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGames
     * @summary Get Game List
     * @request GET:/api/edit/games
     */
    useEditGetGames: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 100
         */
        count?: number;
        /** @format int32 */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ArrayResponseOfGameInfoModel, RequestResponse>(
        doFetch ? [`/api/edit/games`, query] : null,
        options,
      ),

    /**
     * @description Retrieving the game list requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGames
     * @summary Get Game List
     * @request GET:/api/edit/games
     */
    mutateEditGetGames: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 100
         */
        count?: number;
        /** @format int32 */
        skip?: number;
      },
      data?:
        | ArrayResponseOfGameInfoModel
        | Promise<ArrayResponseOfGameInfoModel>,
      options?: MutatorOptions,
    ) =>
      mutate<ArrayResponseOfGameInfoModel>(
        [`/api/edit/games`, query],
        data,
        options,
      ),

    /**
     * @description Import game from a ZIP package; requires Admin permission
     *
     * @tags Edit
     * @name EditImportGame
     * @summary Import game package
     * @request POST:/api/edit/games/import
     */
    editImportGame: (
      data: {
        /** @format binary */
        file?: File | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<number, RequestResponse>({
        path: `/api/edit/games/import`,
        method: "POST",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),

    /**
     * @description Deleting a game challenge flag requires administrator privileges
     *
     * @tags Edit
     * @name EditRemoveFlag
     * @summary Delete Game Challenge Flag
     * @request DELETE:/api/edit/games/{id}/challenges/{cId}/flags/{fId}
     */
    editRemoveFlag: (
      id: number,
      cId: number,
      fId: string,
      params: RequestParams = {},
    ) =>
      this.request<TaskStatus, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/flags/${fId}`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Deleting a game challenge requires administrator privileges
     *
     * @tags Edit
     * @name EditRemoveGameChallenge
     * @summary Delete Game Challenge
     * @request DELETE:/api/edit/games/{id}/challenges/{cId}
     */
    editRemoveGameChallenge: (
      id: number,
      cId: number,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Updating a game challenge attachment requires administrator privileges; only for non-dynamic attachment challenges
     *
     * @tags Edit
     * @name EditUpdateAttachment
     * @summary Update Game Challenge Attachment
     * @request POST:/api/edit/games/{id}/challenges/{cId}/attachment
     */
    editUpdateAttachment: (
      id: number,
      cId: number,
      data: AttachmentCreateModel,
      params: RequestParams = {},
    ) =>
      this.request<number, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/attachment`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Update a division for a game; requires administrator privileges
     *
     * @tags Edit
     * @name EditUpdateDivision
     * @summary Update Division
     * @request PUT:/api/edit/games/{id}/divisions/{divisionId}
     */
    editUpdateDivision: (
      id: number,
      divisionId: number,
      data: DivisionEditModel,
      params: RequestParams = {},
    ) =>
      this.request<Division, RequestResponse>({
        path: `/api/edit/games/${id}/divisions/${divisionId}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Updating a game requires administrator privileges
     *
     * @tags Edit
     * @name EditUpdateGame
     * @summary Update Game
     * @request PUT:/api/edit/games/{id}
     */
    editUpdateGame: (
      id: number,
      data: GameInfoModel,
      params: RequestParams = {},
    ) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Updating a game challenge, requires administrator privileges. Flags are not affected; use Flag-related APIs to modify
     *
     * @tags Edit
     * @name EditUpdateGameChallenge
     * @summary Update Game Challenge Information
     * @request PUT:/api/edit/games/{id}/challenges/{cId}
     */
    editUpdateGameChallenge: (
      id: number,
      cId: number,
      data: ChallengeUpdateModel,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeEditDetailModel, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Updating a game notice requires administrator privileges
     *
     * @tags Edit
     * @name EditUpdateGameNotice
     * @summary Update Game Notice
     * @request PUT:/api/edit/games/{id}/notices/{noticeId}
     */
    editUpdateGameNotice: (
      id: number,
      noticeId: number,
      data: GameNoticeModel,
      params: RequestParams = {},
    ) =>
      this.request<GameNotice, RequestResponse>({
        path: `/api/edit/games/${id}/notices/${noticeId}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this endpoint to update the game poster; administrator privileges required
     *
     * @tags Edit
     * @name EditUpdateGamePoster
     * @summary Update Game Poster
     * @request PUT:/api/edit/games/{id}/poster
     */
    editUpdateGamePoster: (
      id: number,
      data: {
        /** @format binary */
        file?: File | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<string, RequestResponse>({
        path: `/api/edit/games/${id}/poster`,
        method: "PUT",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),

    /**
     * @description Updating a post requires administrator privileges
     *
     * @tags Edit
     * @name EditUpdatePost
     * @summary Update Post
     * @request PUT:/api/edit/posts/{id}
     */
    editUpdatePost: (
      id: string,
      data: PostEditModel,
      params: RequestParams = {},
    ) =>
      this.request<PostDetailModel, RequestResponse>({
        path: `/api/edit/posts/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  ext = {
    /**
     * No description
     *
     * @tags Ext
     * @name ExtCreateFlag
     * @summary Create a new flag for a challenge
     * @request POST:/api/ext/challenges/{challengeId}/flags
     */
    extCreateFlag: (
      challengeId: number,
      data: ChallengeFlagCreateModel,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeFlag, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/flags`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtDeleteFlag
     * @summary Delete a flag
     * @request DELETE:/api/ext/challenges/{challengeId}/flags/{flagId}
     */
    extDeleteFlag: (
      challengeId: number,
      flagId: string,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/flags/${flagId}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetEvents
     * @summary Get big screen events
     * @request GET:/api/ext/events
     */
    extGetEvents: (
      query?: {
        unconsumedOnly?: boolean | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<BigScreenEvent[], RequestResponse>({
        path: `/api/ext/events`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetEvents
     * @summary Get big screen events
     * @request GET:/api/ext/events
     */
    useExtGetEvents: (
      query?: {
        unconsumedOnly?: boolean | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<BigScreenEvent[], RequestResponse>(
        doFetch ? [`/api/ext/events`, query] : null,
        options,
      ),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetEvents
     * @summary Get big screen events
     * @request GET:/api/ext/events
     */
    mutateExtGetEvents: (
      query?: {
        unconsumedOnly?: boolean | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
      },
      data?: BigScreenEvent[] | Promise<BigScreenEvent[]>,
      options?: MutatorOptions,
    ) => mutate<BigScreenEvent[]>([`/api/ext/events`, query], data, options),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetFlag
     * @summary Get a single flag by ID
     * @request GET:/api/ext/challenges/{challengeId}/flags/{flagId}
     */
    extGetFlag: (
      challengeId: number,
      flagId: string,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeFlag, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/flags/${flagId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetFlag
     * @summary Get a single flag by ID
     * @request GET:/api/ext/challenges/{challengeId}/flags/{flagId}
     */
    useExtGetFlag: (
      challengeId: number,
      flagId: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeFlag, RequestResponse>(
        doFetch ? `/api/ext/challenges/${challengeId}/flags/${flagId}` : null,
        options,
      ),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetFlag
     * @summary Get a single flag by ID
     * @request GET:/api/ext/challenges/{challengeId}/flags/{flagId}
     */
    mutateExtGetFlag: (
      challengeId: number,
      flagId: string,
      data?: ChallengeFlag | Promise<ChallengeFlag>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeFlag>(
        `/api/ext/challenges/${challengeId}/flags/${flagId}`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetFlags
     * @summary Get all flags for a challenge
     * @request GET:/api/ext/challenges/{challengeId}/flags
     */
    extGetFlags: (challengeId: number, params: RequestParams = {}) =>
      this.request<ChallengeFlag[], RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/flags`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetFlags
     * @summary Get all flags for a challenge
     * @request GET:/api/ext/challenges/{challengeId}/flags
     */
    useExtGetFlags: (
      challengeId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeFlag[], RequestResponse>(
        doFetch ? `/api/ext/challenges/${challengeId}/flags` : null,
        options,
      ),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetFlags
     * @summary Get all flags for a challenge
     * @request GET:/api/ext/challenges/{challengeId}/flags
     */
    mutateExtGetFlags: (
      challengeId: number,
      data?: ChallengeFlag[] | Promise<ChallengeFlag[]>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeFlag[]>(
        `/api/ext/challenges/${challengeId}/flags`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetPenalty
     * @summary Get penalty configuration for a challenge
     * @request GET:/api/ext/challenges/{challengeId}/penalty
     */
    extGetPenalty: (challengeId: number, params: RequestParams = {}) =>
      this.request<ChallengePenalty, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/penalty`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetPenalty
     * @summary Get penalty configuration for a challenge
     * @request GET:/api/ext/challenges/{challengeId}/penalty
     */
    useExtGetPenalty: (
      challengeId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengePenalty, RequestResponse>(
        doFetch ? `/api/ext/challenges/${challengeId}/penalty` : null,
        options,
      ),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetPenalty
     * @summary Get penalty configuration for a challenge
     * @request GET:/api/ext/challenges/{challengeId}/penalty
     */
    mutateExtGetPenalty: (
      challengeId: number,
      data?: ChallengePenalty | Promise<ChallengePenalty>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengePenalty>(
        `/api/ext/challenges/${challengeId}/penalty`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetSettings
     * @summary Get all competition settings
     * @request GET:/api/ext/settings
     */
    extGetSettings: (params: RequestParams = {}) =>
      this.request<Record<string, string>, RequestResponse>({
        path: `/api/ext/settings`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetSettings
     * @summary Get all competition settings
     * @request GET:/api/ext/settings
     */
    useExtGetSettings: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<Record<string, string>, RequestResponse>(
        doFetch ? `/api/ext/settings` : null,
        options,
      ),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtGetSettings
     * @summary Get all competition settings
     * @request GET:/api/ext/settings
     */
    mutateExtGetSettings: (
      data?: Record<string, string> | Promise<Record<string, string>>,
      options?: MutatorOptions,
    ) => mutate<Record<string, string>>(`/api/ext/settings`, data, options),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtUpdateFlag
     * @summary Update an existing flag
     * @request PUT:/api/ext/challenges/{challengeId}/flags/{flagId}
     */
    extUpdateFlag: (
      challengeId: number,
      flagId: string,
      data: ChallengeFlagUpdateModel,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeFlag, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/flags/${flagId}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtUpdatePenalty
     * @summary Update penalty configuration for a challenge
     * @request PUT:/api/ext/challenges/{challengeId}/penalty
     */
    extUpdatePenalty: (
      challengeId: number,
      data: PenaltyUpdateModel,
      params: RequestParams = {},
    ) =>
      this.request<ChallengePenalty, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/penalty`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Ext
     * @name ExtUpdateSettings
     * @summary Update competition settings
     * @request PUT:/api/ext/settings
     */
    extUpdateSettings: (
      data: Record<string, any>,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/ext/settings`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),
  };
  extSubmission = {
    /**
     * No description
     *
     * @tags ExtSubmission
     * @name ExtSubmissionGetMySubmissions
     * @summary Get current user's submission records
     * @request GET:/api/ext/submissions/mine
     */
    extSubmissionGetMySubmissions: (
      query?: {
        /** @format int32 */
        challengeId?: number | null;
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<SubmissionRecordDto[], RequestResponse>({
        path: `/api/ext/submissions/mine`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags ExtSubmission
     * @name ExtSubmissionGetMySubmissions
     * @summary Get current user's submission records
     * @request GET:/api/ext/submissions/mine
     */
    useExtSubmissionGetMySubmissions: (
      query?: {
        /** @format int32 */
        challengeId?: number | null;
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<SubmissionRecordDto[], RequestResponse>(
        doFetch ? [`/api/ext/submissions/mine`, query] : null,
        options,
      ),

    /**
     * No description
     *
     * @tags ExtSubmission
     * @name ExtSubmissionGetMySubmissions
     * @summary Get current user's submission records
     * @request GET:/api/ext/submissions/mine
     */
    mutateExtSubmissionGetMySubmissions: (
      query?: {
        /** @format int32 */
        challengeId?: number | null;
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: SubmissionRecordDto[] | Promise<SubmissionRecordDto[]>,
      options?: MutatorOptions,
    ) =>
      mutate<SubmissionRecordDto[]>(
        [`/api/ext/submissions/mine`, query],
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags ExtSubmission
     * @name ExtSubmissionGetUserSubmissions
     * @summary Get a specific user's submission records (admin only)
     * @request GET:/api/ext/submissions/user/{userId}
     */
    extSubmissionGetUserSubmissions: (
      userId: string,
      query?: {
        /** @format int32 */
        challengeId?: number | null;
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<SubmissionRecordDto[], RequestResponse>({
        path: `/api/ext/submissions/user/${userId}`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags ExtSubmission
     * @name ExtSubmissionGetUserSubmissions
     * @summary Get a specific user's submission records (admin only)
     * @request GET:/api/ext/submissions/user/{userId}
     */
    useExtSubmissionGetUserSubmissions: (
      userId: string,
      query?: {
        /** @format int32 */
        challengeId?: number | null;
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<SubmissionRecordDto[], RequestResponse>(
        doFetch ? [`/api/ext/submissions/user/${userId}`, query] : null,
        options,
      ),

    /**
     * No description
     *
     * @tags ExtSubmission
     * @name ExtSubmissionGetUserSubmissions
     * @summary Get a specific user's submission records (admin only)
     * @request GET:/api/ext/submissions/user/{userId}
     */
    mutateExtSubmissionGetUserSubmissions: (
      userId: string,
      query?: {
        /** @format int32 */
        challengeId?: number | null;
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: SubmissionRecordDto[] | Promise<SubmissionRecordDto[]>,
      options?: MutatorOptions,
    ) =>
      mutate<SubmissionRecordDto[]>(
        [`/api/ext/submissions/user/${userId}`, query],
        data,
        options,
      ),
  };
  flagImport = {
    /**
     * No description
     *
     * @tags FlagImport
     * @name FlagImportImportChallenge
     * @summary 单题目导入 flags
     * @request POST:/api/ext/challenges/{challengeId}/flags/import
     */
    flagImportImportChallenge: (
      challengeId: number,
      data: {
        /** @format binary */
        file?: File | null;
        mapping?: string | null;
        mode?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<FlagImportResult, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/flags/import`,
        method: "POST",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags FlagImport
     * @name FlagImportImportGame
     * @summary 游戏级导入：按 (仓库, 子模块) 自动创建 GameChallenge 并导入 flags
     * @request POST:/api/ext/games/{gameId}/flags/import
     */
    flagImportImportGame: (
      gameId: number,
      data: {
        /** @format binary */
        file?: File | null;
        mapping?: string | null;
        mode?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<FlagImportResult, RequestResponse>({
        path: `/api/ext/games/${gameId}/flags/import`,
        method: "POST",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags FlagImport
     * @name FlagImportPreviewChallenge
     * @summary 预览单题目导入（不传 mapping 仅返回列名+样本行；传 mapping 试解析 Track 预览）
     * @request POST:/api/ext/challenges/{challengeId}/flags/import/preview
     */
    flagImportPreviewChallenge: (
      challengeId: number,
      data: {
        /** @format binary */
        file?: File | null;
        mapping?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<FlagImportFilePreview, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/flags/import/preview`,
        method: "POST",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags FlagImport
     * @name FlagImportPreviewGame
     * @summary 游戏级预览：按 (仓库, 子模块) 自动建题目的预览
     * @request POST:/api/ext/games/{gameId}/flags/import/preview
     */
    flagImportPreviewGame: (
      gameId: number,
      data: {
        /** @format binary */
        file?: File | null;
        mapping?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<FlagImportFilePreview, RequestResponse>({
        path: `/api/ext/games/${gameId}/flags/import/preview`,
        method: "POST",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),
  };
  game = {
    /**
     * @description Retrieves all challenges of the game; requires User permission and active team participation
     *
     * @tags Game
     * @name GameChallengesWithTeamInfo
     * @summary Get team details in a game
     * @request GET:/api/game/{id}/details
     */
    gameChallengesWithTeamInfo: (id: number, params: RequestParams = {}) =>
      this.request<GameDetailModel, RequestResponse>({
        path: `/api/game/${id}/details`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves all challenges of the game; requires User permission and active team participation
     *
     * @tags Game
     * @name GameChallengesWithTeamInfo
     * @summary Get team details in a game
     * @request GET:/api/game/{id}/details
     */
    useGameChallengesWithTeamInfo: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<GameDetailModel, RequestResponse>(
        doFetch ? `/api/game/${id}/details` : null,
        options,
      ),

    /**
     * @description Retrieves all challenges of the game; requires User permission and active team participation
     *
     * @tags Game
     * @name GameChallengesWithTeamInfo
     * @summary Get team details in a game
     * @request GET:/api/game/{id}/details
     */
    mutateGameChallengesWithTeamInfo: (
      id: number,
      data?: GameDetailModel | Promise<GameDetailModel>,
      options?: MutatorOptions,
    ) => mutate<GameDetailModel>(`/api/game/${id}/details`, data, options),

    /**
     * @description Retrieves game cheat data; requires Monitor permission
     *
     * @tags Game
     * @name GameCheatInfo
     * @summary Get game cheat information
     * @request GET:/api/game/{id}/cheatinfo
     */
    gameCheatInfo: (id: number, params: RequestParams = {}) =>
      this.request<CheatInfoModel[], RequestResponse>({
        path: `/api/game/${id}/cheatinfo`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves game cheat data; requires Monitor permission
     *
     * @tags Game
     * @name GameCheatInfo
     * @summary Get game cheat information
     * @request GET:/api/game/{id}/cheatinfo
     */
    useGameCheatInfo: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<CheatInfoModel[], RequestResponse>(
        doFetch ? `/api/game/${id}/cheatinfo` : null,
        options,
      ),

    /**
     * @description Retrieves game cheat data; requires Monitor permission
     *
     * @tags Game
     * @name GameCheatInfo
     * @summary Get game cheat information
     * @request GET:/api/game/{id}/cheatinfo
     */
    mutateGameCheatInfo: (
      id: number,
      data?: CheatInfoModel[] | Promise<CheatInfoModel[]>,
      options?: MutatorOptions,
    ) => mutate<CheatInfoModel[]>(`/api/game/${id}/cheatinfo`, data, options),

    /**
     * @description Creates a container; requires User permission
     *
     * @tags Game
     * @name GameCreateContainer
     * @summary Creates a container
     * @request POST:/api/game/{id}/container/{challengeId}
     */
    gameCreateContainer: (
      id: number,
      challengeId: number,
      params: RequestParams = {},
    ) =>
      this.request<ContainerInfoModel, RequestResponse>({
        path: `/api/game/${id}/container/${challengeId}`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * @description Deletes a team's traffic packet files for a challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameDeleteAllTeamTraffic
     * @summary Deletes all traffic files
     * @request DELETE:/api/game/captures/{challengeId}/{partId}/all
     */
    gameDeleteAllTeamTraffic: (
      challengeId: number,
      partId: number,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/game/captures/${challengeId}/${partId}/all`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Deletes a container; requires User permission
     *
     * @tags Game
     * @name GameDeleteContainer
     * @summary Deletes a container
     * @request DELETE:/api/game/{id}/container/{challengeId}
     */
    gameDeleteContainer: (
      id: number,
      challengeId: number,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}/container/${challengeId}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Deletes a traffic packet file; requires Monitor permission
     *
     * @tags Game
     * @name GameDeleteTeamTraffic
     * @summary Deletes a traffic file
     * @request DELETE:/api/game/captures/{challengeId}/{partId}/{filename}
     */
    gameDeleteTeamTraffic: (
      challengeId: number,
      partId: number,
      filename: string,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/game/captures/${challengeId}/${partId}/${filename}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Retrieves game event data; requires Monitor permission
     *
     * @tags Game
     * @name GameEvents
     * @summary Get game events
     * @request GET:/api/game/{id}/events
     */
    gameEvents: (
      id: number,
      query?: {
        /**
         * Hide container events
         * @default false
         */
        hideContainer?: boolean;
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<GameEvent[], RequestResponse>({
        path: `/api/game/${id}/events`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves game event data; requires Monitor permission
     *
     * @tags Game
     * @name GameEvents
     * @summary Get game events
     * @request GET:/api/game/{id}/events
     */
    useGameEvents: (
      id: number,
      query?: {
        /**
         * Hide container events
         * @default false
         */
        hideContainer?: boolean;
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<GameEvent[], RequestResponse>(
        doFetch ? [`/api/game/${id}/events`, query] : null,
        options,
      ),

    /**
     * @description Retrieves game event data; requires Monitor permission
     *
     * @tags Game
     * @name GameEvents
     * @summary Get game events
     * @request GET:/api/game/{id}/events
     */
    mutateGameEvents: (
      id: number,
      query?: {
        /**
         * Hide container events
         * @default false
         */
        hideContainer?: boolean;
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: GameEvent[] | Promise<GameEvent[]>,
      options?: MutatorOptions,
    ) => mutate<GameEvent[]>([`/api/game/${id}/events`, query], data, options),

    /**
     * @description Extends container lifetime; requires User permission and can only be extended two hours within ten minutes before expiration
     *
     * @tags Game
     * @name GameExtendContainerLifetime
     * @summary Extends container lifetime
     * @request POST:/api/game/{id}/container/{challengeId}/extend
     */
    gameExtendContainerLifetime: (
      id: number,
      challengeId: number,
      params: RequestParams = {},
    ) =>
      this.request<ContainerInfoModel, RequestResponse>({
        path: `/api/game/${id}/container/${challengeId}/extend`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * @description Retrieves detailed information about the game
     *
     * @tags Game
     * @name GameGame
     * @summary Get detailed game information
     * @request GET:/api/game/{id}
     */
    gameGame: (id: number, params: RequestParams = {}) =>
      this.request<DetailedGameInfoModel, RequestResponse>({
        path: `/api/game/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves detailed information about the game
     *
     * @tags Game
     * @name GameGame
     * @summary Get detailed game information
     * @request GET:/api/game/{id}
     */
    useGameGame: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<DetailedGameInfoModel, RequestResponse>(
        doFetch ? `/api/game/${id}` : null,
        options,
      ),

    /**
     * @description Retrieves detailed information about the game
     *
     * @tags Game
     * @name GameGame
     * @summary Get detailed game information
     * @request GET:/api/game/{id}
     */
    mutateGameGame: (
      id: number,
      data?: DetailedGameInfoModel | Promise<DetailedGameInfoModel>,
      options?: MutatorOptions,
    ) => mutate<DetailedGameInfoModel>(`/api/game/${id}`, data, options),

    /**
     * @description Retrieves game information in specified range
     *
     * @tags Game
     * @name GameGames
     * @summary Get games
     * @request GET:/api/game
     */
    gameGames: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 50
         * @default 10
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfBasicGameInfoModel, RequestResponse>({
        path: `/api/game`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves game information in specified range
     *
     * @tags Game
     * @name GameGames
     * @summary Get games
     * @request GET:/api/game
     */
    useGameGames: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 50
         * @default 10
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ArrayResponseOfBasicGameInfoModel, RequestResponse>(
        doFetch ? [`/api/game`, query] : null,
        options,
      ),

    /**
     * @description Retrieves game information in specified range
     *
     * @tags Game
     * @name GameGames
     * @summary Get games
     * @request GET:/api/game
     */
    mutateGameGames: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 50
         * @default 10
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?:
        | ArrayResponseOfBasicGameInfoModel
        | Promise<ArrayResponseOfBasicGameInfoModel>,
      options?: MutatorOptions,
    ) =>
      mutate<ArrayResponseOfBasicGameInfoModel>(
        [`/api/game`, query],
        data,
        options,
      ),

    /**
     * @description Downloads all traffic packet files for a team and challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetAllTeamTraffic
     * @summary Download all traffic files
     * @request GET:/api/game/captures/{challengeId}/{partId}/all
     */
    gameGetAllTeamTraffic: (
      challengeId: number,
      partId: number,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/game/captures/${challengeId}/${partId}/all`,
        method: "GET",
        ...params,
      }),

    /**
     * @description Retrieves challenge information; requires User permission and active team participation
     *
     * @tags Game
     * @name GameGetChallenge
     * @summary Get challenge information
     * @request GET:/api/game/{id}/challenges/{challengeId}
     */
    gameGetChallenge: (
      id: number,
      challengeId: number,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeDetailModel, RequestResponse>({
        path: `/api/game/${id}/challenges/${challengeId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves challenge information; requires User permission and active team participation
     *
     * @tags Game
     * @name GameGetChallenge
     * @summary Get challenge information
     * @request GET:/api/game/{id}/challenges/{challengeId}
     */
    useGameGetChallenge: (
      id: number,
      challengeId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeDetailModel, RequestResponse>(
        doFetch ? `/api/game/${id}/challenges/${challengeId}` : null,
        options,
      ),

    /**
     * @description Retrieves challenge information; requires User permission and active team participation
     *
     * @tags Game
     * @name GameGetChallenge
     * @summary Get challenge information
     * @request GET:/api/game/{id}/challenges/{challengeId}
     */
    mutateGameGetChallenge: (
      id: number,
      challengeId: number,
      data?: ChallengeDetailModel | Promise<ChallengeDetailModel>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeDetailModel>(
        `/api/game/${id}/challenges/${challengeId}`,
        data,
        options,
      ),

    /**
     * @description Retrieves challenges with traffic capturing enabled; requires Monitor permission
     *
     * @tags Game
     * @name GameGetChallengesWithTrafficCapturing
     * @summary Get challenges with traffic capturing enabled
     * @request GET:/api/game/games/{id}/captures
     */
    gameGetChallengesWithTrafficCapturing: (
      id: number,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeTrafficModel[], RequestResponse>({
        path: `/api/game/games/${id}/captures`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves challenges with traffic capturing enabled; requires Monitor permission
     *
     * @tags Game
     * @name GameGetChallengesWithTrafficCapturing
     * @summary Get challenges with traffic capturing enabled
     * @request GET:/api/game/games/{id}/captures
     */
    useGameGetChallengesWithTrafficCapturing: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeTrafficModel[], RequestResponse>(
        doFetch ? `/api/game/games/${id}/captures` : null,
        options,
      ),

    /**
     * @description Retrieves challenges with traffic capturing enabled; requires Monitor permission
     *
     * @tags Game
     * @name GameGetChallengesWithTrafficCapturing
     * @summary Get challenges with traffic capturing enabled
     * @request GET:/api/game/games/{id}/captures
     */
    mutateGameGetChallengesWithTrafficCapturing: (
      id: number,
      data?: ChallengeTrafficModel[] | Promise<ChallengeTrafficModel[]>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeTrafficModel[]>(
        `/api/game/games/${id}/captures`,
        data,
        options,
      ),

    /**
     * @description Retrieves the list of captured teams for a game challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetChallengeTraffic
     * @summary Get team captures in a challenge
     * @request GET:/api/game/captures/{challengeId}
     */
    gameGetChallengeTraffic: (
      challengeId: number,
      params: RequestParams = {},
    ) =>
      this.request<TeamTrafficModel[], RequestResponse>({
        path: `/api/game/captures/${challengeId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves the list of captured teams for a game challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetChallengeTraffic
     * @summary Get team captures in a challenge
     * @request GET:/api/game/captures/{challengeId}
     */
    useGameGetChallengeTraffic: (
      challengeId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<TeamTrafficModel[], RequestResponse>(
        doFetch ? `/api/game/captures/${challengeId}` : null,
        options,
      ),

    /**
     * @description Retrieves the list of captured teams for a game challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetChallengeTraffic
     * @summary Get team captures in a challenge
     * @request GET:/api/game/captures/{challengeId}
     */
    mutateGameGetChallengeTraffic: (
      challengeId: number,
      data?: TeamTrafficModel[] | Promise<TeamTrafficModel[]>,
      options?: MutatorOptions,
    ) =>
      mutate<TeamTrafficModel[]>(
        `/api/game/captures/${challengeId}`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags Game
     * @name GameGetGameJoinCheckInfo
     * @summary Get check info for joining a game
     * @request GET:/api/game/{id}/check
     */
    gameGetGameJoinCheckInfo: (id: number, params: RequestParams = {}) =>
      this.request<GameJoinCheckInfoModel, RequestResponse>({
        path: `/api/game/${id}/check`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Game
     * @name GameGetGameJoinCheckInfo
     * @summary Get check info for joining a game
     * @request GET:/api/game/{id}/check
     */
    useGameGetGameJoinCheckInfo: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<GameJoinCheckInfoModel, RequestResponse>(
        doFetch ? `/api/game/${id}/check` : null,
        options,
      ),

    /**
     * No description
     *
     * @tags Game
     * @name GameGetGameJoinCheckInfo
     * @summary Get check info for joining a game
     * @request GET:/api/game/{id}/check
     */
    mutateGameGetGameJoinCheckInfo: (
      id: number,
      data?: GameJoinCheckInfoModel | Promise<GameJoinCheckInfoModel>,
      options?: MutatorOptions,
    ) => mutate<GameJoinCheckInfoModel>(`/api/game/${id}/check`, data, options),

    /**
     * @description Retrieves a traffic packet file; requires Monitor permission
     *
     * @tags Game
     * @name GameGetTeamTraffic
     * @summary Get a traffic file
     * @request GET:/api/game/captures/{challengeId}/{partId}/{filename}
     */
    gameGetTeamTraffic: (
      challengeId: number,
      partId: number,
      filename: string,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/game/captures/${challengeId}/${partId}/${filename}`,
        method: "GET",
        ...params,
      }),

    /**
     * @description Retrieves traffic packet files for a team and challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetTeamTrafficAll
     * @summary Get traffic files
     * @request GET:/api/game/captures/{challengeId}/{partId}
     */
    gameGetTeamTrafficAll: (
      challengeId: number,
      partId: number,
      params: RequestParams = {},
    ) =>
      this.request<FileRecord[], RequestResponse>({
        path: `/api/game/captures/${challengeId}/${partId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves traffic packet files for a team and challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetTeamTrafficAll
     * @summary Get traffic files
     * @request GET:/api/game/captures/{challengeId}/{partId}
     */
    useGameGetTeamTrafficAll: (
      challengeId: number,
      partId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<FileRecord[], RequestResponse>(
        doFetch ? `/api/game/captures/${challengeId}/${partId}` : null,
        options,
      ),

    /**
     * @description Retrieves traffic packet files for a team and challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetTeamTrafficAll
     * @summary Get traffic files
     * @request GET:/api/game/captures/{challengeId}/{partId}
     */
    mutateGameGetTeamTrafficAll: (
      challengeId: number,
      partId: number,
      data?: FileRecord[] | Promise<FileRecord[]>,
      options?: MutatorOptions,
    ) =>
      mutate<FileRecord[]>(
        `/api/game/captures/${challengeId}/${partId}`,
        data,
        options,
      ),

    /**
     * @description Retrieves post-game writeup submission information; requires User permission
     *
     * @tags Game
     * @name GameGetWriteup
     * @summary Get writeup information
     * @request GET:/api/game/{id}/writeup
     */
    gameGetWriteup: (id: number, params: RequestParams = {}) =>
      this.request<BasicWriteupInfoModel, RequestResponse>({
        path: `/api/game/${id}/writeup`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves post-game writeup submission information; requires User permission
     *
     * @tags Game
     * @name GameGetWriteup
     * @summary Get writeup information
     * @request GET:/api/game/{id}/writeup
     */
    useGameGetWriteup: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<BasicWriteupInfoModel, RequestResponse>(
        doFetch ? `/api/game/${id}/writeup` : null,
        options,
      ),

    /**
     * @description Retrieves post-game writeup submission information; requires User permission
     *
     * @tags Game
     * @name GameGetWriteup
     * @summary Get writeup information
     * @request GET:/api/game/{id}/writeup
     */
    mutateGameGetWriteup: (
      id: number,
      data?: BasicWriteupInfoModel | Promise<BasicWriteupInfoModel>,
      options?: MutatorOptions,
    ) =>
      mutate<BasicWriteupInfoModel>(`/api/game/${id}/writeup`, data, options),

    /**
     * @description Join a game; requires User permission
     *
     * @tags Game
     * @name GameJoinGame
     * @summary Join a game
     * @request POST:/api/game/{id}
     */
    gameJoinGame: (
      id: number,
      data: GameJoinModel,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Leave a game; requires User permission
     *
     * @tags Game
     * @name GameLeaveGame
     * @summary Leave a game
     * @request DELETE:/api/game/{id}
     */
    gameLeaveGame: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Retrieves game notice data
     *
     * @tags Game
     * @name GameNotices
     * @summary Get game notices
     * @request GET:/api/game/{id}/notices
     */
    gameNotices: (
      id: number,
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @min 0
         * @max 300
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<GameNotice[], RequestResponse>({
        path: `/api/game/${id}/notices`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves game notice data
     *
     * @tags Game
     * @name GameNotices
     * @summary Get game notices
     * @request GET:/api/game/{id}/notices
     */
    useGameNotices: (
      id: number,
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @min 0
         * @max 300
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<GameNotice[], RequestResponse>(
        doFetch ? [`/api/game/${id}/notices`, query] : null,
        options,
      ),

    /**
     * @description Retrieves game notice data
     *
     * @tags Game
     * @name GameNotices
     * @summary Get game notices
     * @request GET:/api/game/{id}/notices
     */
    mutateGameNotices: (
      id: number,
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @min 0
         * @max 300
         * @default 0
         */
        skip?: number;
      },
      data?: GameNotice[] | Promise<GameNotice[]>,
      options?: MutatorOptions,
    ) =>
      mutate<GameNotice[]>([`/api/game/${id}/notices`, query], data, options),

    /**
     * @description Retrieves all participation information of the game; requires Admin permission
     *
     * @tags Game
     * @name GameParticipations
     * @summary Get all game participations
     * @request GET:/api/game/{id}/participations
     */
    gameParticipations: (id: number, params: RequestParams = {}) =>
      this.request<ParticipationInfoModel[], RequestResponse>({
        path: `/api/game/${id}/participations`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves all participation information of the game; requires Admin permission
     *
     * @tags Game
     * @name GameParticipations
     * @summary Get all game participations
     * @request GET:/api/game/{id}/participations
     */
    useGameParticipations: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ParticipationInfoModel[], RequestResponse>(
        doFetch ? `/api/game/${id}/participations` : null,
        options,
      ),

    /**
     * @description Retrieves all participation information of the game; requires Admin permission
     *
     * @tags Game
     * @name GameParticipations
     * @summary Get all game participations
     * @request GET:/api/game/{id}/participations
     */
    mutateGameParticipations: (
      id: number,
      data?: ParticipationInfoModel[] | Promise<ParticipationInfoModel[]>,
      options?: MutatorOptions,
    ) =>
      mutate<ParticipationInfoModel[]>(
        `/api/game/${id}/participations`,
        data,
        options,
      ),

    /**
     * @description Retrieves recent game in three weeks
     *
     * @tags Game
     * @name GameRecentGames
     * @summary Get the recent games
     * @request GET:/api/game/recent
     */
    gameRecentGames: (
      query?: {
        /**
         * Limit of the number of games
         * @format int32
         * @min 0
         * @max 50
         */
        limit?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<BasicGameInfoModel[], RequestResponse>({
        path: `/api/game/recent`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves recent game in three weeks
     *
     * @tags Game
     * @name GameRecentGames
     * @summary Get the recent games
     * @request GET:/api/game/recent
     */
    useGameRecentGames: (
      query?: {
        /**
         * Limit of the number of games
         * @format int32
         * @min 0
         * @max 50
         */
        limit?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<BasicGameInfoModel[], RequestResponse>(
        doFetch ? [`/api/game/recent`, query] : null,
        options,
      ),

    /**
     * @description Retrieves recent game in three weeks
     *
     * @tags Game
     * @name GameRecentGames
     * @summary Get the recent games
     * @request GET:/api/game/recent
     */
    mutateGameRecentGames: (
      query?: {
        /**
         * Limit of the number of games
         * @format int32
         * @min 0
         * @max 50
         */
        limit?: number;
      },
      data?: BasicGameInfoModel[] | Promise<BasicGameInfoModel[]>,
      options?: MutatorOptions,
    ) =>
      mutate<BasicGameInfoModel[]>([`/api/game/recent`, query], data, options),

    /**
     * @description Retrieves the scoreboard data
     *
     * @tags Game
     * @name GameScoreboard
     * @summary Get the scoreboard
     * @request GET:/api/game/{id}/scoreboard
     */
    gameScoreboard: (id: number, params: RequestParams = {}) =>
      this.request<ScoreboardModel, RequestResponse>({
        path: `/api/game/${id}/scoreboard`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves the scoreboard data
     *
     * @tags Game
     * @name GameScoreboard
     * @summary Get the scoreboard
     * @request GET:/api/game/{id}/scoreboard
     */
    useGameScoreboard: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ScoreboardModel, RequestResponse>(
        doFetch ? `/api/game/${id}/scoreboard` : null,
        options,
      ),

    /**
     * @description Retrieves the scoreboard data
     *
     * @tags Game
     * @name GameScoreboard
     * @summary Get the scoreboard
     * @request GET:/api/game/{id}/scoreboard
     */
    mutateGameScoreboard: (
      id: number,
      data?: ScoreboardModel | Promise<ScoreboardModel>,
      options?: MutatorOptions,
    ) => mutate<ScoreboardModel>(`/api/game/${id}/scoreboard`, data, options),

    /**
     * @description Downloads the game scoreboard; requires Monitor permission
     *
     * @tags Game
     * @name GameScoreboardSheet
     * @summary Downloads the scoreboard
     * @request GET:/api/game/{id}/scoreboardsheet
     */
    gameScoreboardSheet: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}/scoreboardsheet`,
        method: "GET",
        ...params,
      }),

    /**
     * @description Queries flag status; requires User permission
     *
     * @tags Game
     * @name GameStatus
     * @summary Queries flag status
     * @request GET:/api/game/{id}/challenges/{challengeId}/status/{submitId}
     */
    gameStatus: (
      id: number,
      challengeId: number,
      submitId: number,
      params: RequestParams = {},
    ) =>
      this.request<AnswerResult, RequestResponse>({
        path: `/api/game/${id}/challenges/${challengeId}/status/${submitId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Queries flag status; requires User permission
     *
     * @tags Game
     * @name GameStatus
     * @summary Queries flag status
     * @request GET:/api/game/{id}/challenges/{challengeId}/status/{submitId}
     */
    useGameStatus: (
      id: number,
      challengeId: number,
      submitId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<AnswerResult, RequestResponse>(
        doFetch
          ? `/api/game/${id}/challenges/${challengeId}/status/${submitId}`
          : null,
        options,
      ),

    /**
     * @description Queries flag status; requires User permission
     *
     * @tags Game
     * @name GameStatus
     * @summary Queries flag status
     * @request GET:/api/game/{id}/challenges/{challengeId}/status/{submitId}
     */
    mutateGameStatus: (
      id: number,
      challengeId: number,
      submitId: number,
      data?: AnswerResult | Promise<AnswerResult>,
      options?: MutatorOptions,
    ) =>
      mutate<AnswerResult>(
        `/api/game/${id}/challenges/${challengeId}/status/${submitId}`,
        data,
        options,
      ),

    /**
     * @description Retrieves game submission data; requires Monitor permission
     *
     * @tags Game
     * @name GameSubmissions
     * @summary Get game submissions
     * @request GET:/api/game/{id}/submissions
     */
    gameSubmissions: (
      id: number,
      query?: {
        /** Submission type */
        type?: AnswerResult | null;
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<Submission[], RequestResponse>({
        path: `/api/game/${id}/submissions`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves game submission data; requires Monitor permission
     *
     * @tags Game
     * @name GameSubmissions
     * @summary Get game submissions
     * @request GET:/api/game/{id}/submissions
     */
    useGameSubmissions: (
      id: number,
      query?: {
        /** Submission type */
        type?: AnswerResult | null;
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<Submission[], RequestResponse>(
        doFetch ? [`/api/game/${id}/submissions`, query] : null,
        options,
      ),

    /**
     * @description Retrieves game submission data; requires Monitor permission
     *
     * @tags Game
     * @name GameSubmissions
     * @summary Get game submissions
     * @request GET:/api/game/{id}/submissions
     */
    mutateGameSubmissions: (
      id: number,
      query?: {
        /** Submission type */
        type?: AnswerResult | null;
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: Submission[] | Promise<Submission[]>,
      options?: MutatorOptions,
    ) =>
      mutate<Submission[]>(
        [`/api/game/${id}/submissions`, query],
        data,
        options,
      ),

    /**
     * @description Downloads all submissions of the game; requires Monitor permission
     *
     * @tags Game
     * @name GameSubmissionSheet
     * @summary Downloads all submissions
     * @request GET:/api/game/{id}/submissionsheet
     */
    gameSubmissionSheet: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}/submissionsheet`,
        method: "GET",
        ...params,
      }),

    /**
     * @description Submits a flag; requires User permission and active team participation
     *
     * @tags Game
     * @name GameSubmit
     * @summary Submits a flag
     * @request POST:/api/game/{id}/challenges/{challengeId}
     */
    gameSubmit: (
      id: number,
      challengeId: number,
      data: FlagSubmitModel,
      params: RequestParams = {},
    ) =>
      this.request<FlagSubmitResponse, RequestResponse>({
        path: `/api/game/${id}/challenges/${challengeId}`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Submits a post-game writeup; requires User permission
     *
     * @tags Game
     * @name GameSubmitWriteup
     * @summary Submits a writeup
     * @request POST:/api/game/{id}/writeup
     */
    gameSubmitWriteup: (
      id: number,
      data: {
        /** @format binary */
        file?: File | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}/writeup`,
        method: "POST",
        body: data,
        type: ContentType.FormData,
        ...params,
      }),
  };
  info = {
    /**
     * @description Get Captcha configuration
     *
     * @tags Info
     * @name InfoGetClientCaptchaInfo
     * @summary Get Captcha configuration
     * @request GET:/api/captcha
     */
    infoGetClientCaptchaInfo: (params: RequestParams = {}) =>
      this.request<ClientCaptchaInfoModel, any>({
        path: `/api/captcha`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get Captcha configuration
     *
     * @tags Info
     * @name InfoGetClientCaptchaInfo
     * @summary Get Captcha configuration
     * @request GET:/api/captcha
     */
    useInfoGetClientCaptchaInfo: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ClientCaptchaInfoModel, any>(
        doFetch ? `/api/captcha` : null,
        options,
      ),

    /**
     * @description Get Captcha configuration
     *
     * @tags Info
     * @name InfoGetClientCaptchaInfo
     * @summary Get Captcha configuration
     * @request GET:/api/captcha
     */
    mutateInfoGetClientCaptchaInfo: (
      data?: ClientCaptchaInfoModel | Promise<ClientCaptchaInfoModel>,
      options?: MutatorOptions,
    ) => mutate<ClientCaptchaInfoModel>(`/api/captcha`, data, options),

    /**
     * @description Get client configuration
     *
     * @tags Info
     * @name InfoGetClientConfig
     * @summary Get client configuration
     * @request GET:/api/config
     */
    infoGetClientConfig: (params: RequestParams = {}) =>
      this.request<ClientConfig, any>({
        path: `/api/config`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get client configuration
     *
     * @tags Info
     * @name InfoGetClientConfig
     * @summary Get client configuration
     * @request GET:/api/config
     */
    useInfoGetClientConfig: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<ClientConfig, any>(doFetch ? `/api/config` : null, options),

    /**
     * @description Get client configuration
     *
     * @tags Info
     * @name InfoGetClientConfig
     * @summary Get client configuration
     * @request GET:/api/config
     */
    mutateInfoGetClientConfig: (
      data?: ClientConfig | Promise<ClientConfig>,
      options?: MutatorOptions,
    ) => mutate<ClientConfig>(`/api/config`, data, options),

    /**
     * @description Get the latest posts
     *
     * @tags Info
     * @name InfoGetLatestPosts
     * @summary Get the latest posts
     * @request GET:/api/posts/latest
     */
    infoGetLatestPosts: (params: RequestParams = {}) =>
      this.request<PostInfoModel[], any>({
        path: `/api/posts/latest`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get the latest posts
     *
     * @tags Info
     * @name InfoGetLatestPosts
     * @summary Get the latest posts
     * @request GET:/api/posts/latest
     */
    useInfoGetLatestPosts: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<PostInfoModel[], any>(
        doFetch ? `/api/posts/latest` : null,
        options,
      ),

    /**
     * @description Get the latest posts
     *
     * @tags Info
     * @name InfoGetLatestPosts
     * @summary Get the latest posts
     * @request GET:/api/posts/latest
     */
    mutateInfoGetLatestPosts: (
      data?: PostInfoModel[] | Promise<PostInfoModel[]>,
      options?: MutatorOptions,
    ) => mutate<PostInfoModel[]>(`/api/posts/latest`, data, options),

    /**
     * @description Get post details
     *
     * @tags Info
     * @name InfoGetPost
     * @summary Get post details
     * @request GET:/api/posts/{id}
     */
    infoGetPost: (id: string, params: RequestParams = {}) =>
      this.request<PostDetailModel, RequestResponse>({
        path: `/api/posts/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get post details
     *
     * @tags Info
     * @name InfoGetPost
     * @summary Get post details
     * @request GET:/api/posts/{id}
     */
    useInfoGetPost: (
      id: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<PostDetailModel, RequestResponse>(
        doFetch ? `/api/posts/${id}` : null,
        options,
      ),

    /**
     * @description Get post details
     *
     * @tags Info
     * @name InfoGetPost
     * @summary Get post details
     * @request GET:/api/posts/{id}
     */
    mutateInfoGetPost: (
      id: string,
      data?: PostDetailModel | Promise<PostDetailModel>,
      options?: MutatorOptions,
    ) => mutate<PostDetailModel>(`/api/posts/${id}`, data, options),

    /**
     * @description Get all posts
     *
     * @tags Info
     * @name InfoGetPosts
     * @summary Get all posts
     * @request GET:/api/posts
     */
    infoGetPosts: (params: RequestParams = {}) =>
      this.request<PostInfoModel[], any>({
        path: `/api/posts`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get all posts
     *
     * @tags Info
     * @name InfoGetPosts
     * @summary Get all posts
     * @request GET:/api/posts
     */
    useInfoGetPosts: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<PostInfoModel[], any>(doFetch ? `/api/posts` : null, options),

    /**
     * @description Get all posts
     *
     * @tags Info
     * @name InfoGetPosts
     * @summary Get all posts
     * @request GET:/api/posts
     */
    mutateInfoGetPosts: (
      data?: PostInfoModel[] | Promise<PostInfoModel[]>,
      options?: MutatorOptions,
    ) => mutate<PostInfoModel[]>(`/api/posts`, data, options),

    /**
     * @description Create Pow Captcha, valid for 5 minutes
     *
     * @tags Info
     * @name InfoPowChallenge
     * @summary Create Pow Captcha
     * @request GET:/api/captcha/powchallenge
     */
    infoPowChallenge: (params: RequestParams = {}) =>
      this.request<HashPowChallenge, RequestResponse>({
        path: `/api/captcha/powchallenge`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Create Pow Captcha, valid for 5 minutes
     *
     * @tags Info
     * @name InfoPowChallenge
     * @summary Create Pow Captcha
     * @request GET:/api/captcha/powchallenge
     */
    useInfoPowChallenge: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<HashPowChallenge, RequestResponse>(
        doFetch ? `/api/captcha/powchallenge` : null,
        options,
      ),

    /**
     * @description Create Pow Captcha, valid for 5 minutes
     *
     * @tags Info
     * @name InfoPowChallenge
     * @summary Create Pow Captcha
     * @request GET:/api/captcha/powchallenge
     */
    mutateInfoPowChallenge: (
      data?: HashPowChallenge | Promise<HashPowChallenge>,
      options?: MutatorOptions,
    ) => mutate<HashPowChallenge>(`/api/captcha/powchallenge`, data, options),
  };
  levelConfig = {
    /**
     * No description
     *
     * @tags LevelConfig
     * @name LevelConfigCreateLevel
     * @request POST:/api/ext/challenges/{challengeId}/levels
     */
    levelConfigCreateLevel: (
      challengeId: number,
      data: LevelConfigCreateModel,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeLevelConfig, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/levels`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags LevelConfig
     * @name LevelConfigDeleteLevel
     * @request DELETE:/api/ext/challenges/{challengeId}/levels/{levelId}
     */
    levelConfigDeleteLevel: (
      challengeId: number,
      levelId: number,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/levels/${levelId}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * No description
     *
     * @tags LevelConfig
     * @name LevelConfigGetLevel
     * @request GET:/api/ext/challenges/{challengeId}/levels/{levelId}
     */
    levelConfigGetLevel: (
      challengeId: number,
      levelId: number,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeLevelConfig, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/levels/${levelId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags LevelConfig
     * @name LevelConfigGetLevel
     * @request GET:/api/ext/challenges/{challengeId}/levels/{levelId}
     */
    useLevelConfigGetLevel: (
      challengeId: number,
      levelId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeLevelConfig, RequestResponse>(
        doFetch ? `/api/ext/challenges/${challengeId}/levels/${levelId}` : null,
        options,
      ),

    /**
     * No description
     *
     * @tags LevelConfig
     * @name LevelConfigGetLevel
     * @request GET:/api/ext/challenges/{challengeId}/levels/{levelId}
     */
    mutateLevelConfigGetLevel: (
      challengeId: number,
      levelId: number,
      data?: ChallengeLevelConfig | Promise<ChallengeLevelConfig>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeLevelConfig>(
        `/api/ext/challenges/${challengeId}/levels/${levelId}`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags LevelConfig
     * @name LevelConfigGetLevels
     * @request GET:/api/ext/challenges/{challengeId}/levels
     */
    levelConfigGetLevels: (challengeId: number, params: RequestParams = {}) =>
      this.request<ChallengeLevelConfig[], RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/levels`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags LevelConfig
     * @name LevelConfigGetLevels
     * @request GET:/api/ext/challenges/{challengeId}/levels
     */
    useLevelConfigGetLevels: (
      challengeId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeLevelConfig[], RequestResponse>(
        doFetch ? `/api/ext/challenges/${challengeId}/levels` : null,
        options,
      ),

    /**
     * No description
     *
     * @tags LevelConfig
     * @name LevelConfigGetLevels
     * @request GET:/api/ext/challenges/{challengeId}/levels
     */
    mutateLevelConfigGetLevels: (
      challengeId: number,
      data?: ChallengeLevelConfig[] | Promise<ChallengeLevelConfig[]>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeLevelConfig[]>(
        `/api/ext/challenges/${challengeId}/levels`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags LevelConfig
     * @name LevelConfigUpdateLevel
     * @request PUT:/api/ext/challenges/{challengeId}/levels/{levelId}
     */
    levelConfigUpdateLevel: (
      challengeId: number,
      levelId: number,
      data: LevelConfigUpdateModel,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeLevelConfig, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/levels/${levelId}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  proxy = {
    /**
     * No description
     *
     * @tags Proxy
     * @name ProxyProxyForInstance
     * @summary Proxy TCP over websocket
     * @request GET:/api/proxy/{id}
     */
    proxyProxyForInstance: (id: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/proxy/${id}`,
        method: "GET",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Proxy
     * @name ProxyProxyForNoInstance
     * @summary Proxy TCP over websocket for admins
     * @request GET:/api/proxy/noinst/{id}
     */
    proxyProxyForNoInstance: (id: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/proxy/noinst/${id}`,
        method: "GET",
        ...params,
      }),
  };
  team = {
    /**
     * @description Interface to accept invitation, requires User permission and not being in team
     *
     * @tags Team
     * @name TeamAccept
     * @summary Accept invitation
     * @request POST:/api/team/accept
     */
    teamAccept: (data: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/team/accept`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to update team avatar, requires User permission and team membership
     *
     * @tags Team
     * @name TeamAvatar
     * @summary Update team avatar
     * @request PUT:/api/team/{id}/avatar
     */
    teamAvatar: (
      id: number,
      data: {
        /** @format binary */
        file?: File | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<string, RequestResponse>({
        path: `/api/team/${id}/avatar`,
        method: "PUT",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),

    /**
     * @description User API for creating teams, each user can only create one team
     *
     * @tags Team
     * @name TeamCreateTeam
     * @summary Create team
     * @request POST:/api/team
     */
    teamCreateTeam: (data: TeamUpdateModel, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description User API for deleting team, requires User permission and team captain status
     *
     * @tags Team
     * @name TeamDeleteTeam
     * @summary Delete team
     * @request DELETE:/api/team/{id}
     */
    teamDeleteTeam: (id: number, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Get basic information of a team by ID
     *
     * @tags Team
     * @name TeamGetBasicInfo
     * @summary Get team information
     * @request GET:/api/team/{id}
     */
    teamGetBasicInfo: (id: number, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get basic information of a team by ID
     *
     * @tags Team
     * @name TeamGetBasicInfo
     * @summary Get team information
     * @request GET:/api/team/{id}
     */
    useTeamGetBasicInfo: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<TeamInfoModel, RequestResponse>(
        doFetch ? `/api/team/${id}` : null,
        options,
      ),

    /**
     * @description Get basic information of a team by ID
     *
     * @tags Team
     * @name TeamGetBasicInfo
     * @summary Get team information
     * @request GET:/api/team/{id}
     */
    mutateTeamGetBasicInfo: (
      id: number,
      data?: TeamInfoModel | Promise<TeamInfoModel>,
      options?: MutatorOptions,
    ) => mutate<TeamInfoModel>(`/api/team/${id}`, data, options),

    /**
     * @description Get basic information of a team based on user
     *
     * @tags Team
     * @name TeamGetTeamsInfo
     * @summary Get current team information
     * @request GET:/api/team
     */
    teamGetTeamsInfo: (params: RequestParams = {}) =>
      this.request<TeamInfoModel[], RequestResponse>({
        path: `/api/team`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get basic information of a team based on user
     *
     * @tags Team
     * @name TeamGetTeamsInfo
     * @summary Get current team information
     * @request GET:/api/team
     */
    useTeamGetTeamsInfo: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<TeamInfoModel[], RequestResponse>(
        doFetch ? `/api/team` : null,
        options,
      ),

    /**
     * @description Get basic information of a team based on user
     *
     * @tags Team
     * @name TeamGetTeamsInfo
     * @summary Get current team information
     * @request GET:/api/team
     */
    mutateTeamGetTeamsInfo: (
      data?: TeamInfoModel[] | Promise<TeamInfoModel[]>,
      options?: MutatorOptions,
    ) => mutate<TeamInfoModel[]>(`/api/team`, data, options),

    /**
     * @description Get team invitation information, must be team creator
     *
     * @tags Team
     * @name TeamInviteCode
     * @summary Get invitation information
     * @request GET:/api/team/{id}/invite
     */
    teamInviteCode: (id: number, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/team/${id}/invite`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get team invitation information, must be team creator
     *
     * @tags Team
     * @name TeamInviteCode
     * @summary Get invitation information
     * @request GET:/api/team/{id}/invite
     */
    useTeamInviteCode: (
      id: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<string, RequestResponse>(
        doFetch ? `/api/team/${id}/invite` : null,
        options,
      ),

    /**
     * @description Get team invitation information, must be team creator
     *
     * @tags Team
     * @name TeamInviteCode
     * @summary Get invitation information
     * @request GET:/api/team/{id}/invite
     */
    mutateTeamInviteCode: (
      id: number,
      data?: string | Promise<string>,
      options?: MutatorOptions,
    ) => mutate<string>(`/api/team/${id}/invite`, data, options),

    /**
     * @description User kick API, kick user with corresponding ID, requires team creator permission
     *
     * @tags Team
     * @name TeamKickUser
     * @summary Kick user
     * @request POST:/api/team/{id}/kick/{userId}
     */
    teamKickUser: (id: number, userId: string, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}/kick/${userId}`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * @description Interface to leave team, requires User permission and being in team
     *
     * @tags Team
     * @name TeamLeave
     * @summary Leave team
     * @request POST:/api/team/{id}/leave
     */
    teamLeave: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/team/${id}/leave`,
        method: "POST",
        ...params,
      }),

    /**
     * @description Team ownership transfer API, must be team creator
     *
     * @tags Team
     * @name TeamTransfer
     * @summary Transfer team ownership
     * @request PUT:/api/team/{id}/transfer
     */
    teamTransfer: (
      id: number,
      data: TeamTransferModel,
      params: RequestParams = {},
    ) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}/transfer`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Interface to update invitation token, must be team creator
     *
     * @tags Team
     * @name TeamUpdateInviteToken
     * @summary Update invitation token
     * @request PUT:/api/team/{id}/invite
     */
    teamUpdateInviteToken: (id: number, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/team/${id}/invite`,
        method: "PUT",
        format: "json",
        ...params,
      }),

    /**
     * @description Team information update API, must be team creator
     *
     * @tags Team
     * @name TeamUpdateTeam
     * @summary Update team information
     * @request PUT:/api/team/{id}
     */
    teamUpdateTeam: (
      id: number,
      data: TeamUpdateModel,
      params: RequestParams = {},
    ) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Perform signature verification
     *
     * @tags Team
     * @name TeamVerifySignature
     * @summary Verify signature
     * @request POST:/api/team/verify
     */
    teamVerifySignature: (
      data: SignatureVerifyModel,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/team/verify`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),
  };
  ticket = {
    /**
     * No description
     *
     * @tags Ticket
     * @name TicketApprove
     * @summary Approve a ticket (admin)
     * @request PUT:/api/ext/tickets/{ticketId}/approve
     */
    ticketApprove: (
      ticketId: string,
      data: TicketReviewModel,
      params: RequestParams = {},
    ) =>
      this.request<TicketDto, RequestResponse>({
        path: `/api/ext/tickets/${ticketId}/approve`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Ticket
     * @name TicketCreate
     * @summary Create a ticket for a penalty submission
     * @request POST:/api/ext/tickets
     */
    ticketCreate: (data: TicketCreateModel, params: RequestParams = {}) =>
      this.request<TicketDto, RequestResponse>({
        path: `/api/ext/tickets`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Ticket
     * @name TicketGetAllTickets
     * @summary Get all tickets (admin)
     * @request GET:/api/ext/tickets
     */
    ticketGetAllTickets: (
      query?: {
        status?: TicketStatus | null;
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<TicketDto[], RequestResponse>({
        path: `/api/ext/tickets`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Ticket
     * @name TicketGetAllTickets
     * @summary Get all tickets (admin)
     * @request GET:/api/ext/tickets
     */
    useTicketGetAllTickets: (
      query?: {
        status?: TicketStatus | null;
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<TicketDto[], RequestResponse>(
        doFetch ? [`/api/ext/tickets`, query] : null,
        options,
      ),

    /**
     * No description
     *
     * @tags Ticket
     * @name TicketGetAllTickets
     * @summary Get all tickets (admin)
     * @request GET:/api/ext/tickets
     */
    mutateTicketGetAllTickets: (
      query?: {
        status?: TicketStatus | null;
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: TicketDto[] | Promise<TicketDto[]>,
      options?: MutatorOptions,
    ) => mutate<TicketDto[]>([`/api/ext/tickets`, query], data, options),

    /**
     * No description
     *
     * @tags Ticket
     * @name TicketGetMyTickets
     * @summary Get current user's tickets
     * @request GET:/api/ext/tickets/mine
     */
    ticketGetMyTickets: (
      query?: {
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
        /** @format int32 */
        gameId?: number | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<TicketDto[], RequestResponse>({
        path: `/api/ext/tickets/mine`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Ticket
     * @name TicketGetMyTickets
     * @summary Get current user's tickets
     * @request GET:/api/ext/tickets/mine
     */
    useTicketGetMyTickets: (
      query?: {
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
        /** @format int32 */
        gameId?: number | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<TicketDto[], RequestResponse>(
        doFetch ? [`/api/ext/tickets/mine`, query] : null,
        options,
      ),

    /**
     * No description
     *
     * @tags Ticket
     * @name TicketGetMyTickets
     * @summary Get current user's tickets
     * @request GET:/api/ext/tickets/mine
     */
    mutateTicketGetMyTickets: (
      query?: {
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
        /** @format int32 */
        gameId?: number | null;
      },
      data?: TicketDto[] | Promise<TicketDto[]>,
      options?: MutatorOptions,
    ) => mutate<TicketDto[]>([`/api/ext/tickets/mine`, query], data, options),

    /**
     * No description
     *
     * @tags Ticket
     * @name TicketGetTicket
     * @summary Get a single ticket by ID
     * @request GET:/api/ext/tickets/{ticketId}
     */
    ticketGetTicket: (ticketId: string, params: RequestParams = {}) =>
      this.request<TicketDto, RequestResponse>({
        path: `/api/ext/tickets/${ticketId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Ticket
     * @name TicketGetTicket
     * @summary Get a single ticket by ID
     * @request GET:/api/ext/tickets/{ticketId}
     */
    useTicketGetTicket: (
      ticketId: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<TicketDto, RequestResponse>(
        doFetch ? `/api/ext/tickets/${ticketId}` : null,
        options,
      ),

    /**
     * No description
     *
     * @tags Ticket
     * @name TicketGetTicket
     * @summary Get a single ticket by ID
     * @request GET:/api/ext/tickets/{ticketId}
     */
    mutateTicketGetTicket: (
      ticketId: string,
      data?: TicketDto | Promise<TicketDto>,
      options?: MutatorOptions,
    ) => mutate<TicketDto>(`/api/ext/tickets/${ticketId}`, data, options),

    /**
     * No description
     *
     * @tags Ticket
     * @name TicketReject
     * @summary Reject a ticket (admin)
     * @request PUT:/api/ext/tickets/{ticketId}/reject
     */
    ticketReject: (
      ticketId: string,
      data: TicketReviewModel,
      params: RequestParams = {},
    ) =>
      this.request<TicketDto, RequestResponse>({
        path: `/api/ext/tickets/${ticketId}/reject`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Ticket
     * @name TicketUploadAttachments
     * @summary Upload image attachments to a pending ticket
     * @request POST:/api/ext/tickets/{ticketId}/attachments
     */
    ticketUploadAttachments: (
      ticketId: string,
      data: {
        files?: File[] | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<TicketAttachmentDto[], RequestResponse>({
        path: `/api/ext/tickets/${ticketId}/attachments`,
        method: "POST",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),
  };
  track = {
    /**
     * No description
     *
     * @tags Track
     * @name TrackCreateTrack
     * @request POST:/api/ext/challenges/{challengeId}/tracks
     */
    trackCreateTrack: (
      challengeId: number,
      data: TrackCreateModel,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeTrack, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/tracks`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Track
     * @name TrackCreateTrackFull
     * @summary 一次调用创建 Track + 多级 Flag（便捷接口，供管理员在比赛进行中动态添加漏洞）
     * @request POST:/api/ext/challenges/{challengeId}/tracks/full
     */
    trackCreateTrackFull: (
      challengeId: number,
      data: TrackFullCreateModel,
      params: RequestParams = {},
    ) =>
      this.request<TrackFullCreateResult, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/tracks/full`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Track
     * @name TrackDeleteTrack
     * @request DELETE:/api/ext/challenges/{challengeId}/tracks/{trackId}
     */
    trackDeleteTrack: (
      challengeId: number,
      trackId: string,
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/tracks/${trackId}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Track
     * @name TrackGetTrack
     * @request GET:/api/ext/challenges/{challengeId}/tracks/{trackId}
     */
    trackGetTrack: (
      challengeId: number,
      trackId: string,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeTrack, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/tracks/${trackId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Track
     * @name TrackGetTrack
     * @request GET:/api/ext/challenges/{challengeId}/tracks/{trackId}
     */
    useTrackGetTrack: (
      challengeId: number,
      trackId: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeTrack, RequestResponse>(
        doFetch ? `/api/ext/challenges/${challengeId}/tracks/${trackId}` : null,
        options,
      ),

    /**
     * No description
     *
     * @tags Track
     * @name TrackGetTrack
     * @request GET:/api/ext/challenges/{challengeId}/tracks/{trackId}
     */
    mutateTrackGetTrack: (
      challengeId: number,
      trackId: string,
      data?: ChallengeTrack | Promise<ChallengeTrack>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeTrack>(
        `/api/ext/challenges/${challengeId}/tracks/${trackId}`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags Track
     * @name TrackGetTracks
     * @request GET:/api/ext/challenges/{challengeId}/tracks
     */
    trackGetTracks: (challengeId: number, params: RequestParams = {}) =>
      this.request<ChallengeTrack[], RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/tracks`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags Track
     * @name TrackGetTracks
     * @request GET:/api/ext/challenges/{challengeId}/tracks
     */
    useTrackGetTracks: (
      challengeId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeTrack[], RequestResponse>(
        doFetch ? `/api/ext/challenges/${challengeId}/tracks` : null,
        options,
      ),

    /**
     * No description
     *
     * @tags Track
     * @name TrackGetTracks
     * @request GET:/api/ext/challenges/{challengeId}/tracks
     */
    mutateTrackGetTracks: (
      challengeId: number,
      data?: ChallengeTrack[] | Promise<ChallengeTrack[]>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeTrack[]>(
        `/api/ext/challenges/${challengeId}/tracks`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags Track
     * @name TrackUpdateTrack
     * @request PUT:/api/ext/challenges/{challengeId}/tracks/{trackId}
     */
    trackUpdateTrack: (
      challengeId: number,
      trackId: string,
      data: TrackUpdateModel,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeTrack, RequestResponse>({
        path: `/api/ext/challenges/${challengeId}/tracks/${trackId}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  zeroDayTicket = {
    /**
     * No description
     *
     * @tags ZeroDayTicket
     * @name ZeroDayTicketApprove
     * @summary Approve a zero-day ticket (admin)
     * @request PUT:/api/ext/zeroday-tickets/{ticketId}/approve
     */
    zeroDayTicketApprove: (
      ticketId: string,
      data: ZeroDayTicketReviewModel,
      params: RequestParams = {},
    ) =>
      this.request<ZeroDayTicketDto, RequestResponse>({
        path: `/api/ext/zeroday-tickets/${ticketId}/approve`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags ZeroDayTicket
     * @name ZeroDayTicketCreate
     * @summary Create a zero-day vulnerability ticket
     * @request POST:/api/ext/zeroday-tickets
     */
    zeroDayTicketCreate: (
      data: ZeroDayTicketCreateModel,
      params: RequestParams = {},
    ) =>
      this.request<ZeroDayTicketDto, RequestResponse>({
        path: `/api/ext/zeroday-tickets`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags ZeroDayTicket
     * @name ZeroDayTicketGetAllTickets
     * @summary Get all zero-day tickets (admin)
     * @request GET:/api/ext/zeroday-tickets
     */
    zeroDayTicketGetAllTickets: (
      query?: {
        status?: ZeroDayTicketStatus | null;
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ZeroDayTicketDto[], RequestResponse>({
        path: `/api/ext/zeroday-tickets`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags ZeroDayTicket
     * @name ZeroDayTicketGetAllTickets
     * @summary Get all zero-day tickets (admin)
     * @request GET:/api/ext/zeroday-tickets
     */
    useZeroDayTicketGetAllTickets: (
      query?: {
        status?: ZeroDayTicketStatus | null;
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ZeroDayTicketDto[], RequestResponse>(
        doFetch ? [`/api/ext/zeroday-tickets`, query] : null,
        options,
      ),

    /**
     * No description
     *
     * @tags ZeroDayTicket
     * @name ZeroDayTicketGetAllTickets
     * @summary Get all zero-day tickets (admin)
     * @request GET:/api/ext/zeroday-tickets
     */
    mutateZeroDayTicketGetAllTickets: (
      query?: {
        status?: ZeroDayTicketStatus | null;
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: ZeroDayTicketDto[] | Promise<ZeroDayTicketDto[]>,
      options?: MutatorOptions,
    ) =>
      mutate<ZeroDayTicketDto[]>(
        [`/api/ext/zeroday-tickets`, query],
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags ZeroDayTicket
     * @name ZeroDayTicketGetMyTickets
     * @summary Get current user's zero-day tickets
     * @request GET:/api/ext/zeroday-tickets/mine
     */
    zeroDayTicketGetMyTickets: (
      query?: {
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ZeroDayTicketDto[], RequestResponse>({
        path: `/api/ext/zeroday-tickets/mine`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags ZeroDayTicket
     * @name ZeroDayTicketGetMyTickets
     * @summary Get current user's zero-day tickets
     * @request GET:/api/ext/zeroday-tickets/mine
     */
    useZeroDayTicketGetMyTickets: (
      query?: {
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ZeroDayTicketDto[], RequestResponse>(
        doFetch ? [`/api/ext/zeroday-tickets/mine`, query] : null,
        options,
      ),

    /**
     * No description
     *
     * @tags ZeroDayTicket
     * @name ZeroDayTicketGetMyTickets
     * @summary Get current user's zero-day tickets
     * @request GET:/api/ext/zeroday-tickets/mine
     */
    mutateZeroDayTicketGetMyTickets: (
      query?: {
        /** @format int32 */
        gameId?: number | null;
        /**
         * @format int32
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: ZeroDayTicketDto[] | Promise<ZeroDayTicketDto[]>,
      options?: MutatorOptions,
    ) =>
      mutate<ZeroDayTicketDto[]>(
        [`/api/ext/zeroday-tickets/mine`, query],
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags ZeroDayTicket
     * @name ZeroDayTicketGetTicket
     * @summary Get a single zero-day ticket by ID
     * @request GET:/api/ext/zeroday-tickets/{ticketId}
     */
    zeroDayTicketGetTicket: (ticketId: string, params: RequestParams = {}) =>
      this.request<ZeroDayTicketDto, RequestResponse>({
        path: `/api/ext/zeroday-tickets/${ticketId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * No description
     *
     * @tags ZeroDayTicket
     * @name ZeroDayTicketGetTicket
     * @summary Get a single zero-day ticket by ID
     * @request GET:/api/ext/zeroday-tickets/{ticketId}
     */
    useZeroDayTicketGetTicket: (
      ticketId: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ZeroDayTicketDto, RequestResponse>(
        doFetch ? `/api/ext/zeroday-tickets/${ticketId}` : null,
        options,
      ),

    /**
     * No description
     *
     * @tags ZeroDayTicket
     * @name ZeroDayTicketGetTicket
     * @summary Get a single zero-day ticket by ID
     * @request GET:/api/ext/zeroday-tickets/{ticketId}
     */
    mutateZeroDayTicketGetTicket: (
      ticketId: string,
      data?: ZeroDayTicketDto | Promise<ZeroDayTicketDto>,
      options?: MutatorOptions,
    ) =>
      mutate<ZeroDayTicketDto>(
        `/api/ext/zeroday-tickets/${ticketId}`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags ZeroDayTicket
     * @name ZeroDayTicketReject
     * @summary Reject a zero-day ticket (admin)
     * @request PUT:/api/ext/zeroday-tickets/{ticketId}/reject
     */
    zeroDayTicketReject: (
      ticketId: string,
      data: ZeroDayTicketReviewModel,
      params: RequestParams = {},
    ) =>
      this.request<ZeroDayTicketDto, RequestResponse>({
        path: `/api/ext/zeroday-tickets/${ticketId}/reject`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
}

const api = new Api({ baseURL: import.meta.env.BASE_URL.replace(/\/$/, '') });
export default api;

export const fetcher = async (
  args: string | [string, Record<string, unknown>],
) => {
  if (typeof args === "string") {
    const response = await api.request({ path: args });
    return response.data;
  } else {
    const [path, query] = args;
    const response = await api.request({ path, query });
    return response.data;
  }
};
