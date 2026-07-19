import {
  Badge,
  Button,
  Divider,
  Group,
  Modal,
  ModalProps,
  SimpleGrid,
  Stack,
  TextInput,
  Text,
  Title,
  useMantineTheme,
  ScrollAreaAutosize,
  Input,
} from '@mantine/core'
import { mdiLightbulbOnOutline, mdiOpenInNew, mdiPackageVariantClosed } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import duration from 'dayjs/plugin/duration'
import React, { FC, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { InstanceEntry } from '@Components/InstanceEntry'
import { ChallengeLockNotice } from '@Components/ChallengeLockNotice'
import { CooldownNotice } from '@Components/CooldownNotice'
import { ContentPlaceholder, InlineMarkdown, Markdown } from '@Components/MarkdownRenderer'
import { useLanguage } from '@Utils/I18n'
import { ChallengeCategoryItemProps } from '@Utils/Shared'
import {
  ChallengeDetailModel,
  ChallengeFlagAnswerType,
  ChallengeType,
  HybridLineFlagInfoModel,
  TrackLevelStatusModel,
} from '@Api'
import classes from '@Styles/ChallengeModal.module.css'
import misc from '@Styles/Misc.module.css'

dayjs.extend(duration)

interface MultiLevelProgressProps {
  tracks: NonNullable<ChallengeDetailModel['tracks']>
  currentLevel?: number
  activeTrackId?: string | null
  solvedCount?: number
  onSelectTrack?: (trackId: string) => void
  startNewTrackActive?: boolean
  onStartNewTrack?: () => void
}

const MultiLevelProgress: FC<MultiLevelProgressProps> = ({ tracks, currentLevel, activeTrackId, solvedCount, onSelectTrack, startNewTrackActive, onStartNewTrack }) => {
  const { t } = useTranslation()

  // 计算已解漏洞数（L1+L2+L3 全解才算 1 个）
  const fullySolvedTrackCount = useMemo(() => {
    return tracks.filter((track) => {
      const levels = track.levels ?? []
      return levels.length > 0 && levels.every((l) => l.status === 'Solved')
    }).length
  }, [tracks])

  const displaySolvedCount = solvedCount ?? fullySolvedTrackCount

  // 取当前活跃 track 的 levels 作为展示基准
  const activeTrack = useMemo(() => {
    if (activeTrackId) {
      const found = tracks.find((t) => t.trackId === activeTrackId)
      if (found) return found
    }
    // 回退：若有任意 track，取第一个；否则空
    return tracks[0]
  }, [tracks, activeTrackId])

  const activeLevels = activeTrack?.levels ?? []
  const hasMultipleTracks = tracks.length > 1

  return (
    <Stack gap="xs" pt="sm">
      <Group justify="space-between" gap="xs" wrap="wrap">
        <Text fw={700} size="sm">
          {t('challenge.level.progress')}
        </Text>
        <Group gap="xs" wrap="wrap" style={{ minWidth: 0 }}>
          <Badge variant="light" color="teal">
            {t('challenge.level.solved_count', { count: displaySolvedCount })}
          </Badge>
          {currentLevel !== undefined &&
            (() => {
              const currentTitle = activeLevels.find((l) => l.level === currentLevel)?.title
              const currentLabel = `${t('challenge.level.current')}: ${
                currentTitle ? `L${currentLevel} - ${currentTitle}` : `L${currentLevel}`
              }`
              return (
                <Badge variant="light" color="indigo" className={classes.levelBadge} title={currentLabel}>
                  {currentLabel}
                </Badge>
              )
            })()}
        </Group>
      </Group>
      {hasMultipleTracks && (
        <Stack gap={4}>
          <Text size="xs" c="dimmed">
            {t('challenge.level.select_track')}
          </Text>
          <Group gap="xs" wrap="wrap">
            {tracks.map((track, idx) => {
              const trackId = track.trackId ?? ''
              const isActive = activeTrackId ? track.trackId === activeTrackId : idx === 0
              const trackLevels = track.levels ?? []
              const allSolved = trackLevels.length > 0 && trackLevels.every((l) => l.status === 'Solved')
              const label = track.title || t('challenge.level.track_fallback', { index: idx + 1 })
              return (
                <Badge
                  key={trackId || idx}
                  variant={isActive ? 'filled' : 'light'}
                  color={allSolved ? 'teal' : isActive ? 'blue' : 'gray'}
                  className={classes.trackBadge}
                  style={{ cursor: onSelectTrack ? 'pointer' : 'default' }}
                  onClick={() => onSelectTrack?.(trackId)}
                  title={`${label}${allSolved ? ' ✓' : ''}`}
                >
                  {label}{allSolved ? ' ✓' : ''}
                </Badge>
              )
            })}
            {onStartNewTrack && (
              <Badge
                variant={startNewTrackActive ? 'filled' : 'light'}
                color={startNewTrackActive ? 'green' : 'gray'}
                style={{ cursor: 'pointer' }}
                onClick={onStartNewTrack}
              >
                {startNewTrackActive
                  ? t('challenge.level.starting_new')
                  : t('challenge.level.new_vulnerability')}
              </Badge>
            )}
          </Group>
          {startNewTrackActive && (
            <Text size="xs" c="green">
              {t('challenge.level.next_submission_l1')}
            </Text>
          )}
        </Stack>
      )}
      {activeLevels.length === 0 ? (
        <Text size="sm" c="dimmed">
          {t('challenge.level.submit_l1_to_start')}
        </Text>
      ) : (
        <Group gap="xs" wrap="wrap" className={classes.levelRow}>
          {activeLevels.map((levelInfo, idx) => {
            const level = levelInfo.level ?? idx + 1
            const status = levelInfo.status ?? 'Unsolved'
            const isCurrent = level === currentLevel

            let color = 'gray'
            let icon = ''
            if (status === 'Solved') {
              color = 'teal'
              icon = ' ✓'
            } else if (status === 'Banned') {
              color = 'red'
              icon = ' 🔴'
            } else if (status === 'Locked') {
              color = 'orange'
              icon = ' ⏸'
            } else if (isCurrent) {
              color = 'blue'
              icon = ` (${t('challenge.level.current')})`
            } else {
              icon = ' 🔒'
            }

            const badgeLabel = levelInfo.title ? `L${level} - ${levelInfo.title}` : `L${level}`

            return (
              <React.Fragment key={level}>
                {idx > 0 && <Text size="sm" c="dimmed">→</Text>}
                <Badge
                  variant={isCurrent ? 'filled' : 'light'}
                  color={color}
                  className={classes.levelBadge}
                  title={`${badgeLabel}${icon}`}
                >
                  {badgeLabel}{icon}
                </Badge>
              </React.Fragment>
            )
          })}
        </Group>
      )}
    </Stack>
  )
}

interface ChallengeDeadlineNoticeProps {
  deadline: dayjs.Dayjs
  onExpiredChange: (expired: boolean) => void
}

const ChallengeDeadlineNotice: FC<ChallengeDeadlineNoticeProps> = ({ deadline, onExpiredChange }) => {
  const { t } = useTranslation()
  const [now, setNow] = useState(dayjs())
  const { locale } = useLanguage()

  useEffect(() => {
    setNow(dayjs())
    const timer = setInterval(() => setNow(dayjs()), 1000)
    return () => clearInterval(timer)
  }, [deadline])

  useEffect(() => {
    onExpiredChange(now.isAfter(deadline))
  }, [now, deadline, onExpiredChange])

  if (now.isAfter(deadline)) {
    return null
  }

  const formattedDeadline = useMemo(() => deadline.locale(locale).format('L LTS'), [deadline, locale])

  const diff = deadline.diff(now)
  const duration = dayjs.duration(diff)
  const countdownText = `${Math.floor(duration.asHours())}:${duration.format('mm:ss')}`

  return (
    <Group gap="xs" justify="space-between" wrap="nowrap">
      <Text fw="bold" size="sm">
        {t('challenge.content.deadline.remaining')}&nbsp;
        <Text span ff="monospace" fw="bold" size="sm" c="brand">
          {countdownText}
        </Text>
      </Text>
      <Text fw="bold" size="xs" c="dimmed">
        {t('challenge.content.deadline.label')}&nbsp;
        <Text span ff="monospace" c="dimmed" fw="bold" size="xs">
          {formattedDeadline}
        </Text>
      </Text>
    </Group>
  )
}

export interface ChallengeModalProps extends ModalProps {
  challenge?: ChallengeDetailModel
  cateData: ChallengeCategoryItemProps
  solved?: boolean
  disabled?: boolean
  gameTitle?: string
  gameEnded?: boolean
  practiceMode?: boolean
  flag: string
  setFlag: (value: string | React.ChangeEvent<any> | null | undefined) => void
  onCreate: () => void
  onExtend: () => void
  onDestroy: () => void
  onSubmitFlag: () => void
  onDownload?: () => void
  onSelectTrack?: (trackId: string) => void
  startNewTrackActive?: boolean
  onStartNewTrack?: () => void
}

export const ChallengeModal: FC<ChallengeModalProps> = (props) => {
  const {
    challenge,
    cateData,
    solved,
    disabled,
    gameTitle,
    gameEnded,
    practiceMode,
    flag,
    setFlag,
    onCreate,
    onExtend,
    onDestroy,
    onDownload,
    onSubmitFlag,
    onSelectTrack,
    startNewTrackActive,
    onStartNewTrack,
    ...modalProps
  } = props
  const { t } = useTranslation()
  const theme = useMantineTheme()
  const { locale } = useLanguage()

  const hybridFlags = useMemo(
    () => [...(challenge?.flags ?? [])].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0)),
    [challenge?.flags]
  )

  const isFlagSolved = (flag: HybridLineFlagInfoModel) => flag.isSolved === true
  const solvedFlagCount = hybridFlags.filter(isFlagSolved).length
  const hasHybridFlags = hybridFlags.length > 0
  const isHybridLine = hasHybridFlags
  const isMultiLevel = (challenge?.tracks?.length ?? 0) > 0
  const allHybridFlagsSolved = hasHybridFlags && solvedFlagCount === hybridFlags.length

  const answerTypeLabel = (type?: ChallengeFlagAnswerType) => {
    switch (type) {
      case ChallengeFlagAnswerType.Exact:
        return t('challenge.content.answer_type.exact')
      case ChallengeFlagAnswerType.LineRange:
        return t('challenge.content.answer_type.line_range')
      case ChallengeFlagAnswerType.MultiLine:
        return t('challenge.content.answer_type.multi_line')
      case ChallengeFlagAnswerType.SourceLocation:
        return t('challenge.content.answer_type.source_location')
      case ChallengeFlagAnswerType.SingleLine:
      default:
        return t('challenge.content.answer_type.single_line')
    }
  }

  const flagDisplayName = (hybridFlag: HybridLineFlagInfoModel, index: number) =>
    hybridFlag.trackId?.slice(0, 8) ||
    t('challenge.content.visible_flag_fallback', { index })

  const placeholders = t('challenge.content.flag_placeholders', {
    returnObjects: true,
  }) as string[]

  const [placeholder, setPlaceholder] = useState('')
  useEffect(() => {
    setPlaceholder(placeholders[Math.floor(Math.random() * placeholders.length)])
  }, [challenge])

  const deadlineTime = useMemo(() => (challenge?.deadline ? dayjs(challenge.deadline) : null), [challenge?.deadline])
  const [isDeadlinePassed, setIsDeadlinePassed] = useState(() => (deadlineTime ? dayjs().isAfter(deadlineTime) : false))

  useEffect(() => {
    setIsDeadlinePassed(deadlineTime ? dayjs().isAfter(deadlineTime) : false)
  }, [deadlineTime])

  const isLimitReached = (challenge?.limit && (challenge.attempts ?? 0) >= challenge.limit) || false

  const isContainer =
    challenge?.type === ChallengeType.StaticContainer || challenge?.type === ChallengeType.DynamicContainer

  const title = (
    <Stack gap="xs">
      <Group wrap="nowrap" w="100%" justify="space-between" gap="sm">
        <Group wrap="nowrap" gap="sm" w="calc(100% - 6.75rem)">
          {cateData && <Icon path={cateData.icon} size={1.2} color={theme.colors[cateData.color][5]} />}
          <Title order={4} lineClamp={1}>
            {challenge?.title ?? ''}
          </Title>
        </Group>
        <Text miw="6rem" fw="bold" ff="monospace" ta="right">
          {challenge?.score ?? 0} pts
        </Text>
      </Group>
      <Divider size="md" color={cateData?.color} />
    </Stack>
  )

  const content = (
    <ScrollAreaAutosize mah="52vh" maw="100%" scrollbars="y" scrollbarSize={6} type="scroll">
      {challenge?.content === undefined ? (
        <ContentPlaceholder />
      ) : (
        <>
          <Markdown source={challenge.content ?? ''} />
          {challenge.hints && challenge.hints.length > 0 && (
            <Stack gap={2} pt="sm">
              {challenge.hints.map((hint) => (
                <Group key={hint} gap="xs" align="flex-start" wrap="nowrap">
                  <Icon path={mdiLightbulbOnOutline} size={0.8} color={theme.colors.yellow[5]} />
                  <InlineMarkdown key={hint} size="sm" maw="calc(100% - 2rem)" source={hint} />
                </Group>
              ))}
            </Stack>
          )}
          {isMultiLevel && challenge?.tracks && (
            <MultiLevelProgress
              tracks={challenge.tracks}
              currentLevel={challenge.currentLevel}
              activeTrackId={challenge.activeTrackId}
              onSelectTrack={onSelectTrack}
              startNewTrackActive={startNewTrackActive}
              onStartNewTrack={onStartNewTrack}
            />
          )}
          {isMultiLevel && (challenge?.challengeLock?.isLocked || challenge?.challengeLock?.bannedTrackId) && (
            <ChallengeLockNotice
              isLocked={challenge.challengeLock.isLocked}
              unlocksAt={challenge.challengeLock.unlocksAt}
              reason={challenge.challengeLock.reason}
              triggerAnswer={challenge.challengeLock.isLocked ? challenge.challengeLock.triggerAnswer : undefined}
              bannedTrackId={challenge.challengeLock.bannedTrackId}
              bannedLevel={challenge.challengeLock.bannedLevel}
              banTriggerAnswer={challenge.challengeLock.banTriggerAnswer}
            />
          )}
          <CooldownNotice cooldown={challenge?.cooldown} />
          {isHybridLine && (
            <Stack gap="xs" className={classes.flagList}>
              <Group justify="space-between" gap="xs">
                <Text fw={700} size="sm">
                  {t('challenge.content.visible_flags')}
                </Text>
                <Badge variant="light" color={allHybridFlagsSolved ? 'teal' : 'gray'}>
                  {solvedFlagCount}/{hybridFlags.length}
                </Badge>
              </Group>
              {hasHybridFlags ? (
                <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="xs">
                  {hybridFlags.map((hybridFlag, index) => {
                    const solved = isFlagSolved(hybridFlag)

                    return (
                      <Stack key={hybridFlag.id ?? index} gap={4} className={classes.flagItem}>
                        <Group gap="xs" justify="space-between" align="flex-start" wrap="nowrap">
                          <Text fw={600} size="sm" lineClamp={1} className={misc.wordBreakAll}>
                            {flagDisplayName(hybridFlag, index + 1)}
                          </Text>
                          <Badge variant="light" color={solved ? 'teal' : 'gray'}>
                            {solved
                              ? t('challenge.content.flag_status.completed')
                              : t('challenge.content.flag_status.pending')}
                          </Badge>
                        </Group>
                        <Group gap={6}>
                          <Badge variant="outline">{answerTypeLabel(hybridFlag.answerType)}</Badge>
                          {hybridFlag.level !== undefined && (
                            <Badge variant="outline" color="indigo">
                              L{hybridFlag.level}
                            </Badge>
                          )}
                        </Group>
                      </Stack>
                    )
                  })}
                </SimpleGrid>
              ) : (
                <Text c="dimmed" size="sm">
                  {t('challenge.content.visible_flags_empty')}
                </Text>
              )}
            </Stack>
          )}
        </>
      )}
    </ScrollAreaAutosize>
  )

  const withDeadline = deadlineTime && !isDeadlinePassed
  const deadline = withDeadline && (
    <ChallengeDeadlineNotice deadline={deadlineTime} onExpiredChange={setIsDeadlinePassed} />
  )

  const withAttachment = !!challenge?.context?.url || onDownload

  const link = challenge?.context?.url
  const baseUrl = import.meta.env.BASE_URL.replace(/\/$/, '')
  const local = link && (link.startsWith('/assets') || (baseUrl !== '' && link.startsWith(`${baseUrl}/assets`)))

  const attachment = withAttachment && (
    <Group gap="xs" justify="flex-start" align="center" wrap="nowrap">
      <Text fw="bold" size="sm">
        {t('challenge.button.download.attachment')}
      </Text>
      <Button
        component="a"
        href={link ?? '#'}
        variant="light"
        size="compact-sm"
        target="_blank"
        rel="noreferrer"
        leftSection={<Icon path={local ? mdiPackageVariantClosed : mdiOpenInNew} size={0.8} />}
        maw="20rem"
        onClick={
          onDownload &&
          ((e: any) => {
            e.preventDefault()
            onDownload()
          })
        }
      >
        {local ? link.split('/').pop() : t('common.content.external_link')}
      </Button>
    </Group>
  )

  const withInstance = isContainer && challenge?.context

  const instance = withInstance && (
    <InstanceEntry
      label={`${challenge.title} @ ${gameTitle}`}
      context={challenge.context!}
      onCreate={onCreate}
      onExtend={onExtend}
      onDestroy={onDestroy}
      disabled={disabled}
    />
  )

  const attemptsInfo = useMemo(() => {
    if (typeof challenge?.attempts !== 'number' || solved) return null

    let content = null
    if (deadlineTime && isDeadlinePassed) {
      content = t('challenge.content.deadline.expired', {
        deadline: deadlineTime.locale(locale).format('L LTS'),
      })
    } else if (challenge?.limit) {
      const remaining = challenge.limit - challenge.attempts
      if (remaining > 0) {
        content = t('challenge.content.attempts.remaining', { remaining })
      } else {
        content = t('challenge.content.attempts.exhausted')
      }
    } else {
      content = t('challenge.content.attempts.count', { count: challenge.attempts })
    }

    return <Input.Label>{content}</Input.Label>
  }, [challenge?.attempts, challenge?.limit, solved, deadlineTime, locale, isDeadlinePassed, t])

  const inputValue = solved
    ? isHybridLine
      ? t('challenge.content.all_flags_completed')
      : t('challenge.content.already_solved')
    : isLimitReached
      ? t('challenge.content.attempts.placeholder')
      : flag

  // Allow submission if deadline not passed OR (game ended AND practice mode enabled)
  const canSubmitDespiteDeadline = !isDeadlinePassed || (gameEnded && practiceMode)
  const inputDisabled = disabled || solved || isLimitReached || !canSubmitDespiteDeadline

  const footer = (
    <Stack gap="xs" className={classes.footer}>
      {(withAttachment || withInstance || withDeadline) && (
        <>
          <Divider mb={attemptsInfo ? '0.2rem' : undefined} />
          {attachment}
          {instance}
          {deadline}
        </>
      )}
      <Divider label={attemptsInfo} my={attemptsInfo ? '-0.4rem' : undefined} />
      <form
        onSubmit={(e) => {
          e.preventDefault()
          if (!solved && canSubmitDespiteDeadline) {
            onSubmitFlag()
          }
        }}
      >
        <Group justify="space-between" gap="sm" align="flex-end">
          <TextInput
            placeholder={
              isHybridLine
                ? t('challenge.content.hybrid_line_placeholder')
                : placeholder
            }
            label={
              isHybridLine
                ? t('challenge.content.hybrid_line_label')
                : undefined
            }
            value={inputValue}
            disabled={inputDisabled}
            onChange={setFlag}
            classNames={{ root: misc.flexGrow, input: misc.ffmono }}
          />
          <Button miw="6rem" type="submit" disabled={inputDisabled}>
            {t('challenge.button.submit_flag')}
          </Button>
        </Group>
      </form>
    </Stack>
  )

  return (
    <Modal.Root
      size="42vw"
      {...modalProps}
      onClose={() => {
        setFlag('')
        modalProps.onClose()
      }}
      centered
      classNames={classes}
    >
      <Modal.Overlay />
      <Modal.Content>
        <Modal.Header>
          <Modal.Title>{title}</Modal.Title>
        </Modal.Header>
        <Modal.Body>{content}</Modal.Body>
        {footer}
      </Modal.Content>
    </Modal.Root>
  )
}
