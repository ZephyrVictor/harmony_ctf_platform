import {
  Alert, Badge, Button, Card, Code, Group, Modal, Select,
  SimpleGrid, Stack, Table, Text, Textarea, TextInput,
} from '@mantine/core'
import { mdiAlertOctagonOutline, mdiGavel } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg, showSuccessMsg } from '@Utils/Shared'
import { Api } from '@Api'
import {
  createBan, deleteBan, listAlertNotices, listBans,
  AlertNotice, BanStateDto,
} from '@Utils/AdminBanApi'
import { searchTeams, TeamSearchResult } from '@Utils/AdminScoreApi'

const Bans: FC = () => {
  const { t } = useTranslation()
  const { id } = useParams()
  const gameId = Number(id)

  const [bans, setBans] = useState<BanStateDto[]>([])
  const [alerts, setAlerts] = useState<AlertNotice[]>([])
  const [loading, setLoading] = useState(false)

  // 手动封禁弹窗状态
  const [createOpen, setCreateOpen] = useState(false)
  const [teamQuery, setTeamQuery] = useState('')
  const [teams, setTeams] = useState<TeamSearchResult[]>([])
  const [challenges, setChallenges] = useState<{ value: string; label: string }[]>([])

  const [form, setForm] = useState({
    teamId: 0,
    challengeId: 0,
    trackId: '',
    level: 3,
    reason: '',
  })
  const [saving, setSaving] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [banList, alertList] = await Promise.all([
        listBans(gameId),
        listAlertNotices(gameId),
      ])
      setBans(banList)
      setAlerts(alertList)
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setLoading(false)
    }
  }, [gameId, t])

  // 加载题目列表（用于手动封禁弹窗的下拉）
  const loadChallenges = useCallback(async () => {
    try {
      const res = await Api.Game.gameChallengesWithTeamInfo(gameId)
      // 从 challenges 里提取题目列表
      const allChals: { value: string; label: string }[] = []
      const challengesMap = res.data?.challenges
      if (challengesMap) {
        Object.entries(challengesMap).forEach(([category, chals]) => {
          chals.forEach((c: any) => {
            allChals.push({ value: String(c.id), label: `${c.title} (${category})` })
          })
        })
      }
      setChallenges(allChals)
    } catch {
      // 忽略，题目列表非必需
    }
  }, [gameId])

  useEffect(() => { load() }, [load])

  const onSearchTeams = useCallback(async () => {
    if (!teamQuery.trim()) return
    try {
      const res = await searchTeams(gameId, teamQuery)
      setTeams(res)
    } catch (e) {
      showErrorMsg(e, t)
    }
  }, [gameId, teamQuery, t])

  const openCreate = () => {
    setCreateOpen(true)
    loadChallenges()
  }

  const submit = async () => {
    if (!form.teamId || !form.challengeId || !form.trackId || !form.reason.trim()) {
      showErrorMsg(new Error(t('admin.ban.error_fields_required')), t)
      return
    }
    setSaving(true)
    try {
      await createBan(gameId, {
        teamId: form.teamId,
        challengeId: form.challengeId,
        trackId: form.trackId,
        level: form.level,
        reason: form.reason,
      })
      showSuccessMsg(t('admin.ban.banned'), t)
      setCreateOpen(false)
      setForm({ teamId: 0, challengeId: 0, trackId: '', level: 3, reason: '' })
      load()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setSaving(false)
    }
  }

  const unban = async (banId: number) => {
    try {
      await deleteBan(gameId, banId)
      showSuccessMsg(t('admin.ban.unbanned'), t)
      load()
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  return (
    <WithGameEditTab>
      <Stack gap="md">
        {/* 告警区 */}
        <Card withBorder p="lg">
          <Group mb="md">
            <Icon path={mdiAlertOctagonOutline} size={1} color="red" />
            <Text fw={700} size="lg">{t('admin.ban.alerts_title')}</Text>
            <Badge color="red">{alerts.length}</Badge>
          </Group>
          {alerts.length === 0 ? (
            <Text c="dimmed" size="sm">{t('admin.ban.no_alerts')}</Text>
          ) : (
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('admin.ban.time')}</Table.Th>
                  <Table.Th>{t('admin.ban.team')}</Table.Th>
                  <Table.Th>{t('admin.ban.challenge')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {alerts.map((a) => (
                  <Table.Tr key={a.id}>
                    <Table.Td>{new Date(a.time).toLocaleString()}</Table.Td>
                    <Table.Td>{a.values?.[0] ?? '-'}</Table.Td>
                    <Table.Td>{a.values?.[1] ?? '-'}</Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          )}
        </Card>

        {/* 封禁列表 */}
        <Card withBorder p="lg">
          <Group mb="md" justify="space-between">
            <Group>
              <Icon path={mdiGavel} size={1} />
              <Text fw={700} size="lg">{t('admin.ban.bans_title')}</Text>
              <Badge>{bans.length}</Badge>
            </Group>
            <Button onClick={openCreate} leftSection={<Icon path={mdiGavel} size={0.7} />}>
              {t('admin.ban.manual_ban')}
            </Button>
          </Group>

          {bans.length === 0 && !loading ? (
            <Text c="dimmed" size="sm">{t('admin.ban.no_bans')}</Text>
          ) : (
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('admin.ban.team')}</Table.Th>
                  <Table.Th>{t('admin.ban.challenge')}</Table.Th>
                  <Table.Th>{t('admin.ban.track')}</Table.Th>
                  <Table.Th>{t('admin.ban.level')}</Table.Th>
                  <Table.Th>{t('admin.ban.reason')}</Table.Th>
                  <Table.Th>{t('admin.ban.type')}</Table.Th>
                  <Table.Th>{t('admin.ban.time')}</Table.Th>
                  <Table.Th>{t('admin.ban.actions')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {bans.map((b) => (
                  <Table.Tr key={b.id}>
                    <Table.Td><Code>#{b.teamId}</Code></Table.Td>
                    <Table.Td><Code>#{b.challengeId}</Code></Table.Td>
                    <Table.Td><Code>{b.trackId.slice(0, 8)}</Code></Table.Td>
                    <Table.Td>L{b.level}</Table.Td>
                    <Table.Td>{b.reason ?? (b.banAnswer ? `trap: ${b.banAnswer}` : '-')}</Table.Td>
                    <Table.Td>
                      <Badge color={b.isManual ? 'red' : 'orange'} variant="light">
                        {b.isManual ? t('admin.ban.manual') : t('admin.ban.trap')}
                      </Badge>
                    </Table.Td>
                    <Table.Td>{new Date(b.bannedAt).toLocaleString()}</Table.Td>
                    <Table.Td>
                      <Button size="xs" color="red" variant="light" onClick={() => unban(b.id)}>
                        {t('admin.ban.unban')}
                      </Button>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          )}
        </Card>
      </Stack>

      {/* 手动封禁弹窗 */}
      <Modal opened={createOpen} onClose={() => setCreateOpen(false)}
             title={t('admin.ban.manual_ban_title')} size="lg">
        <Stack gap="sm">
          <Group>
            <TextInput
              placeholder={t('admin.score.search_placeholder')}
              value={teamQuery}
              onChange={(e) => setTeamQuery(e.target.value)}
              style={{ flex: 1 }}
              onKeyDown={(e) => e.key === 'Enter' && onSearchTeams()}
            />
            <Button onClick={onSearchTeams}>{t('admin.score.search')}</Button>
          </Group>
          {teams.length > 0 && (
            <Select
              label={t('admin.ban.team')}
              data={teams.map((tm) => ({
                value: String(tm.teamId),
                label: `${tm.teamName} (#${tm.teamId})`,
              }))}
              onChange={(v) => v && setForm((f) => ({ ...f, teamId: Number(v) }))}
            />
          )}
          <Select
            label={t('admin.ban.challenge')}
            data={challenges}
            onChange={(v) => v && setForm((f) => ({ ...f, challengeId: Number(v) }))}
          />
          <TextInput
            label={t('admin.ban.track_id')}
            placeholder="00000000-0000-0000-0000-000000000000"
            value={form.trackId}
            onChange={(e) => setForm((f) => ({ ...f, trackId: e.target.value }))}
          />
          <Select
            label={t('admin.ban.level')}
            data={[
              { value: '1', label: 'L1' },
              { value: '2', label: 'L2' },
              { value: '3', label: 'L3' },
            ]}
            value={String(form.level)}
            onChange={(v) => v && setForm((f) => ({ ...f, level: Number(v) }))}
          />
          <Textarea
            label={t('admin.ban.reason')}
            value={form.reason}
            onChange={(e) => setForm((f) => ({ ...f, reason: e.target.value }))}
            required
            autosize
            minRows={2}
          />
          <Alert color="yellow">
            {t('admin.ban.manual_ban_warning')}
          </Alert>
          <Button color="red" onClick={submit} loading={saving}>
            {t('admin.ban.submit')}
          </Button>
        </Stack>
      </Modal>
    </WithGameEditTab>
  )
}

export default Bans
