import {
  Button,
  Divider,
  Group,
  NumberInput,
  Stack,
  Switch,
  Title,
} from '@mantine/core'
import { mdiContentSaveOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AdminPage } from '@Components/admin/AdminPage'
import { showErrorMsg, showSuccessMsg } from '@Utils/Shared'
import { getSettings, updateSettings } from '@Utils/ExtApi'

const ScoringSettings: FC = () => {
  const { t } = useTranslation()

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  // Scoring
  const [minScore, setMinScore] = useState(0)
  const [defaultPenaltyScore, setDefaultPenaltyScore] = useState(0)
  const [maxPenaltyPerSubmission, setMaxPenaltyPerSubmission] = useState(100)

  // Ticket
  const [enableAutoRefund, setEnableAutoRefund] = useState(true)
  const [defaultRefundRatio, setDefaultRefundRatio] = useState(1.0)
  const [maxTicketsPerSubmission, setMaxTicketsPerSubmission] = useState(1)

  // BigScreen
  const [enableBonusEvents, setEnableBonusEvents] = useState(true)

  useEffect(() => {
    getSettings()
      .then((settings) => {
        if (settings['Scoring.MinScore']) setMinScore(Number(settings['Scoring.MinScore']))
        if (settings['Scoring.DefaultPenaltyScore']) setDefaultPenaltyScore(Number(settings['Scoring.DefaultPenaltyScore']))
        if (settings['Scoring.MaxPenaltyPerSubmission']) setMaxPenaltyPerSubmission(Number(settings['Scoring.MaxPenaltyPerSubmission']))
        if (settings['Ticket.EnableAutoRefund']) setEnableAutoRefund(settings['Ticket.EnableAutoRefund'] === 'true')
        if (settings['Ticket.DefaultRefundRatio']) setDefaultRefundRatio(Number(settings['Ticket.DefaultRefundRatio']))
        if (settings['Ticket.MaxTicketsPerSubmission']) setMaxTicketsPerSubmission(Number(settings['Ticket.MaxTicketsPerSubmission']))
        if (settings['BigScreen.EnableBonusEvents']) setEnableBonusEvents(settings['BigScreen.EnableBonusEvents'] === 'true')
      })
      .catch((err) => showErrorMsg(err, t))
      .finally(() => setLoading(false))
  }, [])

  const handleSave = async () => {
    setSaving(true)
    try {
      await updateSettings({
        'Scoring.MinScore': minScore,
        'Scoring.DefaultPenaltyScore': defaultPenaltyScore,
        'Scoring.MaxPenaltyPerSubmission': maxPenaltyPerSubmission,
        'Ticket.EnableAutoRefund': enableAutoRefund,
        'Ticket.DefaultRefundRatio': defaultRefundRatio,
        'Ticket.MaxTicketsPerSubmission': maxTicketsPerSubmission,
        'BigScreen.EnableBonusEvents': enableBonusEvents,
      })
      showSuccessMsg(t('common.content.save_success'), t)
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setSaving(false)
    }
  }

  return (
    <AdminPage
      isLoading={loading}
      head={
        <Group justify="space-between" w="100%">
          <Title order={3}>{t('admin.scoring.title')}</Title>
          <Button
            leftSection={<Icon path={mdiContentSaveOutline} size={1} />}
            onClick={handleSave}
            loading={saving}
          >
            {t('common.content.save')}
          </Button>
        </Group>
      }
    >
      <Stack gap="lg" m="md">
        <Title order={4}>{t('admin.scoring.section.scoring')}</Title>
        <NumberInput
          label={t('admin.scoring.min_score')}
          description={t('admin.scoring.min_score_desc')}
          value={minScore}
          onChange={(v) => setMinScore(Number(v))}
          min={-10000}
          max={0}
        />
        <NumberInput
          label={t('admin.scoring.default_penalty')}
          description={t('admin.scoring.default_penalty_desc')}
          value={defaultPenaltyScore}
          onChange={(v) => setDefaultPenaltyScore(Number(v))}
          min={0}
          max={1000}
        />
        <NumberInput
          label={t('admin.scoring.max_penalty')}
          description={t('admin.scoring.max_penalty_desc')}
          value={maxPenaltyPerSubmission}
          onChange={(v) => setMaxPenaltyPerSubmission(Number(v))}
          min={0}
          max={1000}
        />

        <Divider />

        <Title order={4}>{t('admin.scoring.section.ticket')}</Title>
        <Switch
          label={t('admin.scoring.auto_refund')}
          description={t('admin.scoring.auto_refund_desc')}
          checked={enableAutoRefund}
          onChange={(e) => setEnableAutoRefund(e.currentTarget.checked)}
        />
        <NumberInput
          label={t('admin.scoring.refund_ratio')}
          description={t('admin.scoring.refund_ratio_desc')}
          value={defaultRefundRatio}
          onChange={(v) => setDefaultRefundRatio(Number(v))}
          min={0}
          max={2.0}
          step={0.1}
          decimalScale={1}
        />
        <NumberInput
          label={t('admin.scoring.max_tickets')}
          description={t('admin.scoring.max_tickets_desc')}
          value={maxTicketsPerSubmission}
          onChange={(v) => setMaxTicketsPerSubmission(Number(v))}
          min={1}
          max={10}
        />

        <Divider />

        <Title order={4}>{t('admin.scoring.section.big_screen')}</Title>
        <Switch
          label={t('admin.scoring.enable_bonus')}
          description={t('admin.scoring.enable_bonus_desc')}
          checked={enableBonusEvents}
          onChange={(e) => setEnableBonusEvents(e.currentTarget.checked)}
        />
      </Stack>
    </AdminPage>
  )
}

export default ScoringSettings
