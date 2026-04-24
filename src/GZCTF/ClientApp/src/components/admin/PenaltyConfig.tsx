import { Button, Group, NumberInput, Stack, Text, Title } from '@mantine/core'
import { mdiContentSaveOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getInputNumber, showErrorMsg, showSuccessMsg } from '@Utils/Shared'
import { getPenalty, updatePenalty } from '@Utils/ExtApi'

interface PenaltyConfigProps {
  challengeId: number
}

export const PenaltyConfig: FC<PenaltyConfigProps> = ({ challengeId }) => {
  const { t } = useTranslation()
  const [penaltyScore, setPenaltyScore] = useState(0)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  const load = useCallback(async () => {
    try {
      const data = await getPenalty(challengeId)
      setPenaltyScore(data.penaltyScore)
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setLoading(false)
    }
  }, [challengeId])

  useEffect(() => {
    load()
  }, [load])

  const handleSave = async () => {
    setSaving(true)
    try {
      await updatePenalty(challengeId, { penaltyScore })
      showSuccessMsg(t('common.content.save_success'), t)
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setSaving(false)
    }
  }

  return (
    <Stack gap="md">
      <Title order={5}>{t('admin.penalty.title')}</Title>
      <Text size="sm" c="dimmed">{t('admin.penalty.description')}</Text>
      <Group align="flex-end">
        <NumberInput
          label={t('admin.penalty.score')}
          description={t('admin.penalty.score_desc')}
          value={penaltyScore}
          onChange={(v) => {
              const num = getInputNumber(v)
              if (isNaN(num)) return
              setPenaltyScore(num)
            }}
          min={0}
          disabled={loading}
          style={{ width: 200 }}
        />
        <Button
          leftSection={<Icon path={mdiContentSaveOutline} size={1} />}
          onClick={handleSave}
          loading={saving}
        >
          {t('common.content.save')}
        </Button>
      </Group>
    </Stack>
  )
}
