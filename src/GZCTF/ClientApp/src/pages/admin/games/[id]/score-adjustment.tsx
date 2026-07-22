import {
  Alert, Badge, Button, Card, Code, Group, Modal, NumberInput, Select, SimpleGrid,
  Stack, Table, Text, Textarea, TextInput,
} from '@mantine/core'
import { FC, useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg, showSuccessMsg } from '@Utils/Shared'
import {
  createScoreAdjustment, searchTeams, TeamSearchResult, ZeroDaySeverity,
} from '@Utils/AdminScoreApi'

const SeverityOptions: ZeroDaySeverity[] = ['Critical', 'High', 'Medium', 'Low']

const SeverityMultipliers: Record<ZeroDaySeverity, number> = {
  Critical: 100, High: 60, Medium: 30, Low: 10,
}

const ScoreAdjustment: FC = () => {
  const { t } = useTranslation()
  const { id } = useParams()
  const gameId = Number(id)

  const [query, setQuery] = useState('')
  const [teams, setTeams] = useState<TeamSearchResult[]>([])
  const [searching, setSearching] = useState(false)

  const [adjustOpen, setAdjustOpen] = useState(false)
  const [zeroDayOpen, setZeroDayOpen] = useState(false)
  const [selectedTeam, setSelectedTeam] = useState<TeamSearchResult | null>(null)

  const [delta, setDelta] = useState(0)
  const [reason, setReason] = useState('')
  const [relatedInfo, setRelatedInfo] = useState('')
  const [severity, setSeverity] = useState<ZeroDaySeverity>('Critical')
  const [saving, setSaving] = useState(false)

  const getZeroDayDelta = (team: TeamSearchResult | null, value: ZeroDaySeverity) =>
    Math.max(1, team?.zeroDayBaseScore ?? 1) * SeverityMultipliers[value]

  const onSearch = useCallback(async () => {
    if (!query.trim()) return
    setSearching(true)
    try {
      const res = await searchTeams(gameId, query)
      setTeams(res)
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setSearching(false)
    }
  }, [gameId, query, t])

  const openAdjust = (team: TeamSearchResult) => {
    setSelectedTeam(team)
    setDelta(0)
    setReason('')
    setRelatedInfo('')
    setAdjustOpen(true)
  }

  const openZeroDay = (team: TeamSearchResult) => {
    setSelectedTeam(team)
    setSeverity('Critical')
    setDelta(getZeroDayDelta(team, 'Critical'))
    setReason('')
    setRelatedInfo('')
    setZeroDayOpen(true)
  }

  const onSeverityChange = (v: string | null) => {
    if (!v) return
    const nextSeverity = v as ZeroDaySeverity
    setSeverity(nextSeverity)
    setDelta(getZeroDayDelta(selectedTeam, nextSeverity))
  }

  const submit = async (category: 'Manual' | 'ZeroDayBonus') => {
    if (!selectedTeam) return
    if (delta === 0) {
      showErrorMsg(new Error(t('admin.score.error_delta_zero')), t)
      return
    }
    if (!reason.trim()) {
      showErrorMsg(new Error(t('admin.score.error_reason_required')), t)
      return
    }
    setSaving(true)
    try {
      await createScoreAdjustment(gameId, {
        teamId: selectedTeam.teamId,
        delta,
        reason,
        category,
        zeroDaySeverity: category === 'ZeroDayBonus' ? severity : null,
        relatedInfo: relatedInfo || null,
      })
      showSuccessMsg(t('admin.score.adjust_success'), t)
      setAdjustOpen(false)
      setZeroDayOpen(false)
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setSaving(false)
    }
  }

  return (
    <WithGameEditTab>
      <Card withBorder p="lg">
        <Group mb="md">
          <TextInput
            placeholder={t('admin.score.search_placeholder')}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            style={{ flex: 1 }}
            onKeyDown={(e) => e.key === 'Enter' && onSearch()}
          />
          <Button onClick={onSearch} loading={searching}>
            {t('admin.score.search')}
          </Button>
        </Group>

        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>{t('admin.score.team_name')}</Table.Th>
              <Table.Th>{t('admin.score.members')}</Table.Th>
              <Table.Th>{t('admin.score.nday_score')}</Table.Th>
              <Table.Th>{t('admin.score.token')}</Table.Th>
              <Table.Th>{t('admin.score.actions')}</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {teams.length === 0 && (
              <Table.Tr>
                <Table.Td colSpan={5} align="center" style={{ opacity: 0.6 }}>
                  {t('admin.score.no_results')}
                </Table.Td>
              </Table.Tr>
            )}
            {teams.map((tm) => (
              <Table.Tr key={tm.teamId}>
                <Table.Td>{tm.teamName}</Table.Td>
                <Table.Td>{tm.members.join(', ') || '-'}</Table.Td>
                <Table.Td>
                  <Group gap="xs">
                    <Text size="sm">
                      {tm.ndayScore}
                      {tm.zeroDayNdayScoreRequirement > 0 ? ` / ${tm.zeroDayNdayScoreRequirement}` : ''}
                    </Text>
                    <Badge color={tm.canReceiveZeroDayBonus ? 'teal' : 'yellow'} variant="light">
                      {tm.canReceiveZeroDayBonus
                        ? t('admin.score.zeroday_gate_passed')
                        : t('admin.score.zeroday_gate_unmet')}
                    </Badge>
                  </Group>
                </Table.Td>
                <Table.Td><Code>{tm.teamToken}</Code></Table.Td>
                <Table.Td>
                  <Group gap="xs">
                    <Button size="xs" variant="light" onClick={() => openAdjust(tm)}>
                      {t('admin.score.adjust')}
                    </Button>
                    <Button size="xs" variant="light" color="grape" onClick={() => openZeroDay(tm)}>
                      {t('admin.score.zero_day_bonus')}
                    </Button>
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </Card>

      {/* 手动调分弹窗 */}
      <Modal opened={adjustOpen} onClose={() => setAdjustOpen(false)}
             title={t('admin.score.adjust_title')}>
        <SimpleGrid cols={1} spacing="sm">
          <Text size="sm" c="dimmed">
            {t('admin.score.team')}: <b>{selectedTeam?.teamName}</b>
          </Text>
          <NumberInput
            label={t('admin.score.delta')}
            value={delta}
            onChange={(v) => setDelta(Number(v) || 0)}
            required
          />
          <Textarea
            label={t('admin.score.reason')}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            required
            autosize
            minRows={2}
          />
          <Button onClick={() => submit('Manual')} loading={saving}>
            {t('admin.score.submit')}
          </Button>
        </SimpleGrid>
      </Modal>

      {/* 0day 加分弹窗 */}
      <Modal opened={zeroDayOpen} onClose={() => setZeroDayOpen(false)}
             title={t('admin.score.zero_day_bonus_title')}>
        <SimpleGrid cols={1} spacing="sm">
          <Text size="sm" c="dimmed">
            {t('admin.score.team')}: <b>{selectedTeam?.teamName}</b>
          </Text>
          {selectedTeam && !selectedTeam.canReceiveZeroDayBonus && (
            <Alert color="yellow">
              {t('admin.score.zeroday_bonus_gate_warning', {
                current: selectedTeam.ndayScore,
                required: selectedTeam.zeroDayNdayScoreRequirement,
              })}
            </Alert>
          )}
          <Select
            label={t('admin.score.severity')}
            data={SeverityOptions}
            value={severity}
            onChange={onSeverityChange}
            required
          />
          <NumberInput
            label={t('admin.score.delta')}
            value={delta}
            description={t('admin.score.delta_calculated', {
              base: selectedTeam?.zeroDayBaseScore ?? 1,
              multiplier: SeverityMultipliers[severity],
            })}
            readOnly
            required
          />
          <TextInput
            label={t('admin.score.cve_id')}
            value={relatedInfo}
            onChange={(e) => setRelatedInfo(e.target.value)}
            placeholder="CVE-2024-XXXX"
          />
          <Textarea
            label={t('admin.score.reason')}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            required
            autosize
            minRows={2}
            placeholder={t('admin.score.zero_day_reason_placeholder')}
          />
          <Button color="grape" onClick={() => submit('ZeroDayBonus')} loading={saving}>
            {t('admin.score.submit')}
          </Button>
        </SimpleGrid>
      </Modal>
    </WithGameEditTab>
  )
}

export default ScoreAdjustment
