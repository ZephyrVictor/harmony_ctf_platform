import { Alert, Badge, Group, Progress, SimpleGrid, Stack, Text } from '@mantine/core'
import { mdiCounter, mdiLockAlertOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ChallengeSubmissionControlStatus, SubmissionQuotaStatus } from '@Api'

export interface SubmissionControlNoticeProps {
  control?: ChallengeSubmissionControlStatus | null
}

interface QuotaItem {
  key: keyof ChallengeSubmissionControlStatus
  label: string
  quota?: SubmissionQuotaStatus | null
}

export const getSubmissionControlBlockReason = (control?: ChallengeSubmissionControlStatus | null, now = Date.now()) => {
  if (!control) return null

  const quotas: Array<[string, SubmissionQuotaStatus | null | undefined]> = [
    ['今日比赛提交次数已达上限', control.gameDailyTeam],
    ['本题提交次数已达上限', control.challengeTotalTeam],
    ['今日本题提交次数已达上限', control.challengeDailyTeam],
  ]

  const quotaBlock = quotas.find(([, quota]) => quota?.exceeded)
  if (quotaBlock) return quotaBlock[0]

  const cooldownUntil = control.cooldown?.cooldownUntil
  const untilMs = cooldownUntil
    ? typeof cooldownUntil === 'number'
      ? cooldownUntil
      : dayjs(cooldownUntil).valueOf()
    : 0

  return untilMs > now ? '题目冷却中' : null
}

export const SubmissionControlNotice: FC<SubmissionControlNoticeProps> = ({ control }) => {
  const { t } = useTranslation()
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    if (!control?.cooldown?.cooldownUntil) return
    const timer = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(timer)
  }, [control?.cooldown?.cooldownUntil])

  const items = useMemo<QuotaItem[]>(() => {
    if (!control) return []

    const quotaItems: QuotaItem[] = [
      {
        key: 'gameDailyTeam',
        label: t('challenge.submission_control.game_daily_team', { defaultValue: '今日比赛提交' }),
        quota: control.gameDailyTeam,
      },
      {
        key: 'challengeDailyTeam',
        label: t('challenge.submission_control.challenge_daily_team', { defaultValue: '今日本题提交' }),
        quota: control.challengeDailyTeam,
      },
      {
        key: 'challengeTotalTeam',
        label: t('challenge.submission_control.challenge_total_team', { defaultValue: '本题队伍总提交' }),
        quota: control.challengeTotalTeam,
      },
    ]

    return quotaItems.filter((item) => item.quota && !item.quota.unlimited)
  }, [control, t])

  if (items.length === 0) return null

  const blockedReason = getSubmissionControlBlockReason(control, now)

  return (
    <Stack gap="xs" pt="sm">
      {blockedReason && (
        <Alert
          icon={<Icon path={mdiLockAlertOutline} size={1} />}
          color="red"
          variant="light"
          title={t('challenge.submission_control.blocked', { defaultValue: '提交已受限' })}
        >
          <Text size="sm">{blockedReason}</Text>
        </Alert>
      )}
      <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="xs">
        {items.map(({ key, label, quota }) => {
          const limit = quota?.limit ?? 0
          const used = quota?.used ?? 0
          const remaining = quota?.remaining ?? 0
          const pct = limit > 0 ? Math.min(100, (used / limit) * 100) : 0
          const resetText = quota?.resetsAt
            ? new Intl.DateTimeFormat('zh-CN', {
                timeZone: 'Asia/Shanghai',
                hour: '2-digit',
                minute: '2-digit',
                hour12: false,
              }).format(new Date(quota.resetsAt))
            : null

          return (
            <Alert key={key} icon={<Icon path={mdiCounter} size={0.9} />} color={remaining <= 0 ? 'red' : 'blue'} variant="light">
              <Stack gap={6}>
                <Group justify="space-between" gap="xs" wrap="nowrap">
                  <Text size="sm" fw={600}>{label}</Text>
                  <Badge color={remaining <= 0 ? 'red' : 'blue'} variant="light">
                    {used} / {limit}
                  </Badge>
                </Group>
                <Progress value={pct} size="sm" color={remaining <= 0 ? 'red' : 'blue'} />
                <Group justify="space-between" gap="xs">
                  <Text size="xs" c="dimmed">
                    {t('challenge.submission_control.remaining', { defaultValue: '剩余' })}: {remaining}
                  </Text>
                  {resetText && (
                    <Text size="xs" c="dimmed">
                      {t('challenge.submission_control.resets_at', { defaultValue: '重置' })}: {resetText}
                    </Text>
                  )}
                </Group>
              </Stack>
            </Alert>
          )
        })}
      </SimpleGrid>
    </Stack>
  )
}
