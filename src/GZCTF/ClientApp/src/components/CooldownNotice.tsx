import { Alert, Badge, Group, Progress, Stack, Text } from '@mantine/core'
import { mdiTimerSand } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

export interface CooldownInfo {
  wrongCount?: number
  wrongAnswerLimit?: number
  cooldownMinutes?: number
  cooldownUntil?: number | string | null
}

export interface CooldownNoticeProps {
  cooldown?: CooldownInfo | null
}

export const CooldownNotice: FC<CooldownNoticeProps> = ({ cooldown }) => {
  const { t } = useTranslation()
  const [now, setNow] = useState(() => Date.now())

  // 倒计时刷新（每秒）
  useEffect(() => {
    if (!cooldown?.cooldownUntil) return
    const id = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(id)
  }, [cooldown?.cooldownUntil])

  if (!cooldown) return null

  const untilMs = cooldown.cooldownUntil
    ? typeof cooldown.cooldownUntil === 'number'
      ? cooldown.cooldownUntil
      : dayjs(cooldown.cooldownUntil).valueOf()
    : 0

  const isCoolingDown = untilMs > now
  const wrongCount = cooldown.wrongCount ?? 0
  const limit = cooldown.wrongAnswerLimit ?? 0
  const minutes = cooldown.cooldownMinutes ?? 0

  // 没配置限流 → 不显示
  if (minutes <= 0 && !isCoolingDown) return null

  if (isCoolingDown) {
    const remainingMs = untilMs - now
    const mm = Math.floor(remainingMs / 60000)
    const ss = Math.floor((remainingMs % 60000) / 1000)
    const remainingStr = `${String(mm).padStart(2, '0')}:${String(ss).padStart(2, '0')}`
    return (
      <Stack gap="xs" pt="sm">
        <Alert
          icon={<Icon path={mdiTimerSand} size={1} />}
          color="orange"
          variant="filled"
          title={t('challenge.cooldown.title', { defaultValue: '题目冷却中' })}
        >
          <Stack gap={4}>
            <Text size="sm">
              {t('challenge.cooldown.remaining', { defaultValue: '剩余时间' })}: <strong>{remainingStr}</strong>
            </Text>
            <Text size="sm" c="white">
              {t('challenge.cooldown.hint', { defaultValue: '答错次数达阈值，请稍后再试。' })}
            </Text>
          </Stack>
        </Alert>
      </Stack>
    )
  }

  // 不在冷却中：显示进度（如果有 limit）
  if (limit <= 0) return null
  const pct = Math.min(100, (wrongCount / limit) * 100)
  const isWarning = wrongCount >= Math.floor(limit * 0.8)

  return (
    <Stack gap="xs" pt="sm">
      <Alert
        icon={<Icon path={mdiTimerSand} size={1} />}
        color={isWarning ? 'yellow' : 'blue'}
        variant="light"
        title={t('challenge.cooldown.counter_title', { defaultValue: '答错计数' })}
      >
        <Stack gap={6}>
          <Group gap="xs">
            <Badge variant="light" color={isWarning ? 'red' : 'blue'}>
              {wrongCount} / {limit}
            </Badge>
            <Text size="xs" c="dimmed">
              {t('challenge.cooldown.counter_hint', { defaultValue: '答错达阈值后进入冷却' })}
            </Text>
          </Group>
          <Progress value={pct} size="sm" color={isWarning ? 'red' : 'blue'} />
        </Stack>
      </Alert>
    </Stack>
  )
}
