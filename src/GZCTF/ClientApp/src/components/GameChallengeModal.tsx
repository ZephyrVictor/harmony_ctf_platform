import { ModalProps } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { notifications, showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiLoading } from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ChallengeModal } from '@Components/ChallengeModal'
import { encryptApiData } from '@Utils/Crypto'
import { showErrorMsg } from '@Utils/Shared'
import { ChallengeCategoryItemProps } from '@Utils/Shared'
import { useConfig } from '@Hooks/useConfig'
import api, { AnswerResult, ChallengeType, SubmissionType } from '@Api'

interface GameChallengeModalProps extends ModalProps {
  gameId: number
  gameTitle: string
  gameEnded: boolean
  practiceMode?: boolean
  cateData: ChallengeCategoryItemProps
  title: string
  score: number
  challengeId: number
  status?: SubmissionType
}

export const GameChallengeModal: FC<GameChallengeModalProps> = (props) => {
  const { gameId, gameTitle, gameEnded, practiceMode, challengeId, cateData, status, title, score, ...modalProps } =
    props

  const { data: challenge, mutate } = api.game.useGameGetChallenge(gameId, challengeId, {
    refreshInterval: 120 * 1000,
  })

  const { config } = useConfig()
  const { t } = useTranslation()

  const wrongFlagHints = t('challenge.content.wrong_flag_hints', {
    returnObjects: true,
  }) as string[]

  const isDynamic =
    challenge?.type === ChallengeType.StaticContainer || challenge?.type === ChallengeType.DynamicContainer
  const isHybridLine = (challenge?.flags?.length ?? 0) > 0

  // D6+D7+D8: 多级 flag 状态
  const isMultiLevel = (challenge?.tracks?.length ?? 0) > 0
  const currentLevel = challenge?.currentLevel ?? 1
  const [currentTrackId, setCurrentTrackId] = useState<string | undefined>(undefined)

  // D8: 优先使用后端的 activeTrackId，回退到客户端推导
  useEffect(() => {
    if (!isMultiLevel || !challenge) return
    if (challenge.activeTrackId) {
      // 服务端给出活跃 trackId，但只在客户端尚未缓存或缓存不一致时更新
      if (currentTrackId !== challenge.activeTrackId) {
        // 仅当当前 level > 1 且没有缓存 trackId 时才用服务端的，避免覆盖刚提交的 L1 响应
        if (currentLevel > 1 && !currentTrackId) {
          setCurrentTrackId(challenge.activeTrackId)
        }
      }
      return
    }
    // 无 activeTrackId：清空缓存
    if (!currentTrackId) return
  }, [challenge, isMultiLevel, currentLevel, currentTrackId])

  const [disabled, setDisabled] = useState(false)
  const [submitId, setSubmitId] = useState(0)
  const [submitMessage, setSubmitMessage] = useState<string | undefined>(undefined)
  const [submitImmediateResult, setSubmitImmediateResult] = useState<AnswerResult | null>(null)
  const [lastIsTrackComplete, setLastIsTrackComplete] = useState(false)
  const [flag, setFlag] = useInputState('')
  const [solvedChallengeId, setSolvedChallengeId] = useState<number | null>(null)
  const [startNewTrack, setStartNewTrack] = useState(false)

  const isLimitReached = (challenge?.limit && (challenge.attempts ?? 0) >= challenge.limit) || false
  // D6: currentLevel > 1 但 currentTrackId 尚未恢复时，禁用提交
  const canSubmitMultiLevel = !isMultiLevel || currentLevel === 1 || currentTrackId !== undefined

  const onCreate = async () => {
    if (!challengeId || disabled) return
    setDisabled(true)

    try {
      const res = await api.game.gameCreateContainer(gameId, challengeId)
      mutate({
        ...challenge,
        context: {
          ...challenge?.context,
          closeTime: res.data.expectStopAt,
          instanceEntry: res.data.entry,
        },
      })
      showNotification({
        color: 'teal',
        title: t('challenge.notification.instance.created.title'),
        message: t('challenge.notification.instance.created.message'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const requestDestroy = async () => {
    try {
      await mutate()

      if (!challenge?.context?.instanceEntry) return

      await api.game.gameDeleteContainer(gameId, challengeId)
      mutate({
        ...challenge,
        context: {
          ...challenge?.context,
          closeTime: null,
          instanceEntry: null,
        },
      })
      showNotification({
        color: 'teal',
        title: t('challenge.notification.instance.destroyed.title'),
        message: t('challenge.notification.instance.destroyed.message'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  const onDestroy = async () => {
    if (!challengeId || disabled) return
    setDisabled(true)

    await requestDestroy()

    setDisabled(false)
  }

  const onExtend = async () => {
    if (!challengeId || disabled) return
    setDisabled(true)

    try {
      const res = await api.game.gameExtendContainerLifetime(gameId, challengeId)
      mutate({
        ...challenge,
        context: {
          ...challenge?.context,
          closeTime: res.data.expectStopAt,
        },
      })
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const onSubmit = async () => {
    if (!challengeId || !flag) {
      showNotification({
        color: 'red',
        message: t('challenge.notification.flag.empty'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    if (!canSubmitMultiLevel) {
      showNotification({
        color: 'red',
        message: t('challenge.notification.flag.multi_level_track_required'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setDisabled(true)

    try {
      // D6: L1 不传 trackId；L2+ 用缓存的 trackId
      // Bug #4 fix: startNewTrack overrides to L1 submission (no trackId) so player can start a new vulnerability chain
      const submitLevel = isMultiLevel ? (startNewTrack ? 1 : currentLevel) : 1
      const submitTrackId = isMultiLevel && submitLevel > 1 ? currentTrackId : undefined

      const res = await api.game.gameSubmit(gameId, challengeId, {
        flag: await encryptApiData(t, flag, config.apiPublicKey),
        level: submitLevel,
        trackId: submitTrackId,
      })
      const submitId = res.data.id
      const message = res.data.message ?? undefined
      const immediateResult =
        message && (
          message.toLowerCase().includes('already been submitted')
          || message.includes('已提交过')
          || message.includes('请继续提交下一 Level')
        )
          ? AnswerResult.FlagSubmitted
          : null
      // D6+D8: 缓存响应返回的 trackId（= activeTrackId）用于 L2+ 提交
      if (res.data.trackId) {
        setCurrentTrackId(res.data.trackId)
      }
      // Bug #4: reset startNewTrack after submission (server's currentLevel will be correct after mutate)
      setStartNewTrack(false)
      setLastIsTrackComplete(res.data.isTrackComplete === true)
      // D8: 触发锁/封禁时显示详细提示
      if (res.data.challengeLocked) {
        showNotification({
          color: 'red',
          title: t('challenge.notification.flag.locked.title'),
          message: res.data.unlocksAt
            ? `${t('challenge.notification.flag.locked.unlock_at')}: ${new Date(res.data.unlocksAt).toLocaleString()}${res.data.triggerAnswer ? ` | ${res.data.triggerAnswer}` : ''}`
            : t('challenge.notification.flag.locked.permanent'),
          icon: <Icon path={mdiClose} size={1} />,
          autoClose: 10000,
        })
      }
      if (res.data.bannedTrackId) {
        showNotification({
          color: 'red',
          title: t('challenge.notification.flag.banned.title'),
          message: `${t('challenge.notification.flag.banned.level')} L${res.data.bannedLevel ?? '-'} ${t('challenge.notification.flag.banned.banned')}${res.data.triggerAnswer ? ` | ${res.data.triggerAnswer}` : ''}`,
          icon: <Icon path={mdiClose} size={1} />,
          autoClose: 10000,
        })
      }
      // D8: 重复提交提示
      if (message && message.toLowerCase().includes('already been submitted')) {
        showNotification({
          color: 'yellow',
          title: t('challenge.notification.flag.duplicate.title'),
          message: t('challenge.notification.flag.duplicate.message'),
          autoClose: 6000,
        })
      }
      setSubmitId(submitId)
      setSubmitMessage(message)
      setSubmitImmediateResult(immediateResult)
      notifications.clean()
      showNotification({
        id: 'flag-submitted',
        color: 'orange',
        title: t('challenge.notification.flag.submitted.title'),
        message: t('challenge.notification.flag.submitted.message'),
        loading: true,
        autoClose: false,
      })

      const nxt = (challenge?.attempts ?? 0) + 1
      const attempts = challenge?.limit && challenge.limit > 0 ? Math.min(nxt, challenge.limit) : nxt

      mutate({
        attempts,
        ...challenge,
        cooldown: res.data.cooldown ?? challenge?.cooldown,
        submissionControl: res.data.submissionControl ?? challenge?.submissionControl,
      })
      return
    } catch (e) {
      showErrorMsg(e, t)
      mutate()
      setDisabled(false)
      return
    }
  }

  useEffect(() => {
    if (!submitId) return

    const polling = setInterval(async () => {
      try {
        const res = await api.game.gameStatus(gameId, challengeId, submitId)
        if (res.data !== AnswerResult.FlagSubmitted) {
          setDisabled(false)
          setFlag('')
          setSubmitImmediateResult(null)
          checkDataFlag(submitId, res.data)
          clearInterval(polling)
        } else if (submitImmediateResult === AnswerResult.FlagSubmitted) {
          setDisabled(false)
          setFlag('')
          checkDataFlag(submitId, AnswerResult.FlagSubmitted)
          setSubmitImmediateResult(null)
          clearInterval(polling)
        } else {
          return
        }
      } catch (err) {
        setDisabled(false)
        setFlag('')
        showErrorMsg(err, t)
        clearInterval(polling)
      }
    }, 500)

    return () => clearInterval(polling)
  }, [submitId, submitImmediateResult])

  useEffect(() => {
    if (challengeId !== solvedChallengeId) return

    if (status !== SubmissionType.Unaccepted && status !== undefined) {
      // status has been updated, reset solved challenge id
      setSolvedChallengeId(null)
    }
  }, [status, challengeId, challenge])

  useEffect(() => {
    const control = challenge?.submissionControl
    if (!control) return

    const timestamps = [
      control.gameDailyTeam?.resetsAt,
      control.challengeDailyTeam?.resetsAt,
      control.cooldown?.cooldownUntil,
    ]
      .map((value) => (typeof value === 'number' ? value : value ? new Date(value).getTime() : 0))
      .filter((value) => value > Date.now())

    if (timestamps.length === 0) return

    const nextRefresh = Math.min(...timestamps)
    const timer = setTimeout(() => mutate(), nextRefresh - Date.now() + 1000)
    return () => clearTimeout(timer)
  }, [challenge?.submissionControl, mutate])

  const checkDataFlag = async (id: number, data: string) => {
    if (data === AnswerResult.Accepted) {
      if (!isHybridLine && !isMultiLevel) {
        setSolvedChallengeId(challengeId)
      }
      updateNotification({
        id: 'flag-submitted',
        color: 'teal',
        title: t('challenge.notification.flag.accepted.title'),
        message: gameEnded
          ? t('challenge.notification.flag.accepted.ended')
          : t('challenge.notification.flag.accepted.message'),
        icon: <Icon path={mdiCheck} size={1} />,
        autoClose: 8000,
        loading: false,
      })
      if (isHybridLine || isMultiLevel) {
        // D6+D8: 多级提交成功后 mutate 刷新 currentLevel + activeTrackId
        await mutate()
        // D8: 检查是否完成了整个 track（L3 已解）
        if (lastIsTrackComplete) {
          showNotification({
            color: 'green',
            title: t('challenge.notification.flag.track_complete.title', { defaultValue: 'Vulnerability solved' }),
            message: t('challenge.notification.flag.track_complete.message', {
              defaultValue: 'You have solved all levels of this vulnerability!',
            }),
            icon: <Icon path={mdiCheck} size={1} />,
            autoClose: 10000,
          })
        }
      } else {
        if (isDynamic && challenge.context?.instanceEntry) await requestDestroy()
        props.onClose()
      }
    } else if (data === AnswerResult.WrongAnswer) {
      updateNotification({
        id: 'flag-submitted',
        color: 'red',
        title: t('challenge.notification.flag.wrong'),
        message: submitMessage ?? wrongFlagHints[Math.floor(Math.random() * wrongFlagHints.length)],
        icon: <Icon path={mdiClose} size={1} />,
        autoClose: 8000,
        loading: false,
      })
    } else if (data === AnswerResult.FlagSubmitted) {
      updateNotification({
        id: 'flag-submitted',
        color: 'yellow',
        title: t('challenge.notification.flag.duplicate.title'),
        message: submitMessage ?? t('challenge.notification.flag.duplicate.message'),
        icon: <Icon path={mdiCheck} size={1} />,
        autoClose: 8000,
        loading: false,
      })
      if (isHybridLine || isMultiLevel) {
        await mutate()
      }
    } else {
      updateNotification({
        id: 'flag-submitted',
        color: 'yellow',
        title: t('challenge.notification.flag.unknown.title'),
        message: t('challenge.notification.flag.unknown.message', {
          id,
        }),
        icon: <Icon path={mdiLoading} size={1} />,
        autoClose: false,
        withCloseButton: true,
      })
    }
  }

  return (
    <ChallengeModal
      {...modalProps}
      gameTitle={gameTitle}
      challenge={challenge ?? { title, score }}
      cateData={cateData}
      solved={
        // D5: 多级 flag 题目永远不标记为整体 solved
        isMultiLevel
          ? false
          : isHybridLine
            ? challenge?.allEnabledFlagsSolved === true
            : (status !== SubmissionType.Unaccepted && status !== undefined) || solvedChallengeId === challengeId
      }
      flag={flag}
      setFlag={setFlag}
      onCreate={onCreate}
      onDestroy={onDestroy}
      onSubmitFlag={onSubmit}
      disabled={disabled || isLimitReached || !canSubmitMultiLevel}
      onExtend={onExtend}
      gameEnded={gameEnded}
      practiceMode={practiceMode}
      onSelectTrack={(trackId) => {
        // Issue #5: allow user to switch active track for L2+ submission
        setCurrentTrackId(trackId)
      }}
      startNewTrackActive={startNewTrack}
      onStartNewTrack={() => {
        // Bug #4: toggle "start new track" mode. When enabling, clear the cached
        // trackId so the next submission is treated as L1 of a new chain.
        // When disabling, also clear the cached trackId — the useEffect will
        // restore it from challenge.activeTrackId on the next render.
        if (startNewTrack) {
          setStartNewTrack(false)
          setCurrentTrackId(undefined)
        } else {
          setCurrentTrackId(undefined)
          setStartNewTrack(true)
        }
      }}
    />
  )
}
