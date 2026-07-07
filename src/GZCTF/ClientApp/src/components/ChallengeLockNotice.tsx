import { Alert, Stack, Text } from '@mantine/core'
import { mdiAlertCircleOutline, mdiLockOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'

export interface ChallengeLockNoticeProps {
  isLocked?: boolean
  unlocksAt?: string | number | null
  reason?: string | null
  triggerAnswer?: string | null
  bannedTrackId?: string | null
  bannedLevel?: number | null
  banTriggerAnswer?: string | null
}

export const ChallengeLockNotice: FC<ChallengeLockNoticeProps> = ({
  isLocked,
  unlocksAt,
  reason,
  triggerAnswer,
  bannedTrackId,
  bannedLevel,
  banTriggerAnswer,
}) => {
  const { t } = useTranslation()

  return (
    <Stack gap="xs" pt="sm">
      {isLocked && (
        <Alert
          icon={<Icon path={mdiLockOutline} size={1} />}
          color="orange"
          variant="filled"
          title={t('challenge.lock.title')}
        >
          <Stack gap={4}>
            <Text size="sm">
              {t('challenge.lock.unlock_at')}:{' '}
              {unlocksAt ? dayjs(typeof unlocksAt === 'number' ? unlocksAt * 1000 : unlocksAt).format('YYYY-MM-DD HH:mm:ss') : '-'}
            </Text>
            {reason && (
              <Text size="sm" c="white">
                {t('challenge.lock.reason')}: {reason}
              </Text>
            )}
            {triggerAnswer && (
              <Text size="sm" c="white" ff="monospace">
                {t('challenge.lock.trigger_answer')}: {triggerAnswer}
              </Text>
            )}
          </Stack>
        </Alert>
      )}
      {bannedTrackId && (
        <Alert
          icon={<Icon path={mdiAlertCircleOutline} size={1} />}
          color="red"
          variant="filled"
          title={t('challenge.ban.title')}
        >
          <Stack gap={4}>
            <Text size="sm">
              {t('challenge.ban.level_banned')} L{bannedLevel ?? '-'}{' '}
              {t('challenge.ban.banned')}
            </Text>
            {banTriggerAnswer && (
              <Text size="sm" c="white" ff="monospace">
                {t('challenge.ban.trigger_answer')}: {banTriggerAnswer}
              </Text>
            )}
          </Stack>
        </Alert>
      )}
    </Stack>
  )
}
