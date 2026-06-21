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

  // D6: 多级 flag 状态
  const isMultiLevel = (challenge?.tracks?.length ?? 0) > 0
  const currentLevel = challenge?.currentLevel ?? 1
  const [currentTrackId, setCurrentTrackId] = useState<string | undefined>(undefined)

  // D6 fallback: 页面刷新后从 GetChallenge 推导 trackId
  // 找"前置 Level 已解 + 当前 Level 未解"的 Track
  useEffect(() => {
    if (!isMultiLevel || !challenge?.tracks) return
    if (currentTrackId) return // 已有 trackId，不重新推导
    if (currentLevel <= 1) return // L1 不需要 trackId

    const prevLevel = currentLevel - 1
    const activeTrack = challenge.tracks.find(
      (track) =>
        track.isEnabled !== false &&
        track.levels?.find((l) => l.level === prevLevel)?.status === 'Solved' &&
        track.levels?.find((l) => l.level === currentLevel)?.status !== 'Solved'
    )
    if (activeTrack) {
      setCurrentTrackId(activeTrack.trackId)
    }
  }, [challenge, isMultiLevel, currentLevel, currentTrackId])

  const [disabled, setDisabled] = useState(false)
  const [submitId, setSubmitId] = useState(0)
  const [submitMessage, setSubmitMessage] = useState<string | undefined>(undefined)
  const [flag, setFlag] = useInputState('')
  const [solvedChallengeId, setSolvedChallengeId] = useState<number | null>(null)

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
      const submitLevel = isMultiLevel ? currentLevel : 1
      const submitTrackId = isMultiLevel && currentLevel > 1 ? currentTrackId : undefined

      const res = await api.game.gameSubmit(gameId, challengeId, {
        flag: await encryptApiData(t, flag, config.apiPublicKey),
        level: submitLevel,
        trackId: submitTrackId,
      })
      const submitId = res.data.id
      const message = res.data.message ?? undefined
      // D6: 缓存 L1 响应返回的 trackId 用于 L2+ 提交
      if (res.data.trackId) {
        setCurrentTrackId(res.data.trackId)
      }
      setSubmitId(submitId)
      setSubmitMessage(message)
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
      })
      return
    } catch (e) {
      showErrorMsg(e, t)
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
          checkDataFlag(submitId, res.data)
          clearInterval(polling)
        }
      } catch (err) {
        setDisabled(false)
        setFlag('')
        showErrorMsg(err, t)
        clearInterval(polling)
      }
    }, 500)

    return () => clearInterval(polling)
  }, [submitId])

  useEffect(() => {
    if (challengeId !== solvedChallengeId) return

    if (status !== SubmissionType.Unaccepted && status !== undefined) {
      // status has been updated, reset solved challenge id
      setSolvedChallengeId(null)
    }
  }, [status, challengeId, challenge])

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
        // D6: 多级提交成功后 mutate 刷新 currentLevel，等玩家输入下一级
        await mutate()
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
    />
  )
}
