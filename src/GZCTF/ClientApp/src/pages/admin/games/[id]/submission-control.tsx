import {
  Alert, Badge, Button, Card, Group, Modal, NumberInput, ScrollArea, Select, SimpleGrid, Stack, Table, Text, Title,
} from '@mantine/core'
import { mdiCogSyncOutline, mdiRefresh, mdiTuneVertical } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useCallback, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { getInputNumber, showErrorMsg, showSuccessMsg } from '@Utils/Shared'
import {
  getSubmissionControl,
  getSubmissionControlTeamDetail,
  SubmissionControlChallengeConfig,
  SubmissionControlLimit,
  SubmissionControlLimitMode,
  SubmissionControlOverview,
  SubmissionControlTeamDetail,
  updateSubmissionControl,
} from '@Utils/AdminSubmissionControlApi'
import { ParticipationStatus } from '@Api'

const toDateTime = (value?: string | number | null) => {
  if (!value) return '-'
  const date = typeof value === 'number' ? new Date(value) : new Date(value)
  return Number.isNaN(date.getTime()) ? '-' : date.toLocaleString()
}

const statusColor = (status: ParticipationStatus) => {
  switch (status) {
    case ParticipationStatus.Accepted:
      return 'green'
    case ParticipationStatus.Pending:
      return 'yellow'
    case ParticipationStatus.Rejected:
      return 'red'
    case ParticipationStatus.Suspended:
      return 'orange'
    default:
      return 'gray'
  }
}

const num = (value?: number | null) => value ?? 0

const modeOptions: Array<{ value: SubmissionControlLimitMode, label: string }> = [
  { value: 'InheritDefault', label: '继承默认' },
  { value: 'Custom', label: '自定义' },
  { value: 'Unlimited', label: '不限制' },
]

const effectiveText = (limit: SubmissionControlLimit, trackCount: number) => {
  if (limit.mode === 'Unlimited' || limit.effectiveValue <= 0) return '不限制'
  if (limit.mode === 'Custom') return `自定义 ${limit.effectiveValue}`
  const factor = trackCount > 0 ? limit.defaultValue / trackCount : 0
  return `${trackCount} × ${factor} = ${limit.effectiveValue}`
}

const withEffectiveLimit = (
  limit: SubmissionControlLimit,
  trackCount: number,
  factor: number,
): SubmissionControlLimit => {
  const defaultValue = trackCount > 0 && factor > 0 ? Math.min(trackCount * factor, 2147483647) : 0
  const customValue = num(limit.customValue)
  const effectiveValue = limit.mode === 'Custom'
    ? customValue
    : limit.mode === 'Unlimited'
      ? 0
      : defaultValue

  return { ...limit, customValue, defaultValue, effectiveValue }
}

const LimitEditor: FC<{
  limit: SubmissionControlLimit
  trackCount: number
  factor: number
  onChange: (limit: SubmissionControlLimit) => void
}> = ({ limit, trackCount, factor, onChange }) => {
  const displayed = withEffectiveLimit(limit, trackCount, factor)

  return (
  <Stack gap={4} miw={170}>
    <Select
      size="xs"
      data={modeOptions}
      value={displayed.mode}
      allowDeselect={false}
      onChange={(value) => {
        if (!value) return
        onChange(withEffectiveLimit({ ...displayed, mode: value as SubmissionControlLimitMode }, trackCount, factor))
      }}
    />
    {displayed.mode === 'Custom' && (
      <NumberInput
        size="xs"
        min={0}
        value={displayed.customValue}
        onChange={(v) => onChange(withEffectiveLimit({ ...displayed, customValue: getInputNumber(v) }, trackCount, factor))}
      />
    )}
    <Badge variant="light" leftSection={<Icon path={mdiTuneVertical} size={0.75} />}>
      {effectiveText(displayed, trackCount)}
    </Badge>
  </Stack>
  )
}

const SubmissionControlPage: FC = () => {
  const { t } = useTranslation()
  const { id } = useParams()
  const gameId = Number(id)

  const [data, setData] = useState<SubmissionControlOverview | null>(null)
  const [challenges, setChallenges] = useState<SubmissionControlChallengeConfig[]>([])
  const [dailyTeamLimit, setDailyTeamLimit] = useState(0)
  const [cooldownMinutes, setCooldownMinutes] = useState(0)
  const [cooldownFactor, setCooldownFactor] = useState(5)
  const [challengeDailyFactor, setChallengeDailyFactor] = useState(20)
  const [challengeTotalFactor, setChallengeTotalFactor] = useState(200)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [detailLoading, setDetailLoading] = useState(false)
  const [detail, setDetail] = useState<SubmissionControlTeamDetail | null>(null)

  const applyData = (next: SubmissionControlOverview) => {
    setData(next)
    setDailyTeamLimit(next.game.dailyTeamSubmissionLimit)
    setCooldownMinutes(next.game.cooldownMinutes)
    setCooldownFactor(next.game.challengeCooldownWrongAnswerLimitFactor)
    setChallengeDailyFactor(next.game.challengeDailyTeamSubmissionLimitFactor)
    setChallengeTotalFactor(next.game.challengeTotalTeamSubmissionLimitFactor)
    setChallenges(next.challenges)
  }

  const load = useCallback(async () => {
    if (!Number.isFinite(gameId)) return
    setLoading(true)
    try {
      applyData(await getSubmissionControl(gameId))
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setLoading(false)
    }
  }, [gameId, t])

  useEffect(() => { load() }, [load])

  useEffect(() => {
    const timer = setInterval(() => {
      load()
    }, 15000)
    return () => clearInterval(timer)
  }, [load])

  const updateChallenge = (challengeId: number, patch: Partial<SubmissionControlChallengeConfig>) => {
    setChallenges((items) => items.map((item) => (
      item.challengeId === challengeId ? { ...item, ...patch } : item
    )))
  }

  const buildChallengesPayload = () => challenges.map((challenge) => ({
    challengeId: challenge.challengeId,
    dailyTeamSubmissionLimit: num(withEffectiveLimit(
      challenge.dailyTeamSubmission,
      challenge.trackCount,
      challengeDailyFactor,
    ).customValue),
    totalTeamSubmissionLimit: num(withEffectiveLimit(
      challenge.totalTeamSubmission,
      challenge.trackCount,
      challengeTotalFactor,
    ).customValue),
    cooldownWrongAnswerLimitOverride: num(withEffectiveLimit(
      challenge.cooldownWrongAnswerLimit,
      challenge.trackCount,
      cooldownFactor,
    ).customValue),
    cooldownWrongAnswerLimitMode: challenge.cooldownWrongAnswerLimit.mode,
    dailyTeamSubmissionLimitMode: challenge.dailyTeamSubmission.mode,
    totalTeamSubmissionLimitMode: challenge.totalTeamSubmission.mode,
  }))

  const save = async (applyDefaultsToAllChallenges = false) => {
    setSaving(true)
    try {
      const next = await updateSubmissionControl(gameId, {
        dailyTeamSubmissionLimit: dailyTeamLimit,
        cooldownMinutes,
        challengeCooldownWrongAnswerLimitFactor: cooldownFactor,
        challengeDailyTeamSubmissionLimitFactor: challengeDailyFactor,
        challengeTotalTeamSubmissionLimitFactor: challengeTotalFactor,
        applyDefaultsToAllChallenges,
        challenges: buildChallengesPayload(),
      })
      applyData(next)
      showSuccessMsg(t('admin.submission_control.saved', { defaultValue: '提交控制配置已保存' }), t)
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setSaving(false)
    }
  }

  const openDetail = async (teamId: number) => {
    setDetailLoading(true)
    try {
      setDetail(await getSubmissionControlTeamDetail(gameId, teamId))
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDetailLoading(false)
    }
  }

  const applyDefaults = async () => {
    await save(true)
  }

  const resetText = useMemo(() => toDateTime(data?.dailyResetAt), [data?.dailyResetAt])

  return (
    <WithGameEditTab
      isLoading={loading}
      head={
        <Group justify="flex-end" w="100%">
          <Button variant="light" leftSection={<Icon path={mdiRefresh} size={1} />} onClick={load} loading={loading}>
            {t('admin.submission_control.refresh', { defaultValue: '刷新' })}
          </Button>
          <Button leftSection={<Icon path={mdiCogSyncOutline} size={1} />} onClick={() => save()} loading={saving}>
            {t('admin.submission_control.save', { defaultValue: '保存配置' })}
          </Button>
        </Group>
      }
    >
      <Stack gap="md">
        <Card withBorder p="lg">
          <Group justify="space-between" align="flex-start" mb="md">
            <Stack gap={2}>
              <Title order={4}>{t('admin.submission_control.title', { defaultValue: '提交控制' })}</Title>
              <Text size="sm" c="dimmed">
                {t('admin.submission_control.reset_hint', {
                  defaultValue: '每日配额按 PRC 自然日统计，下一次重置',
                })}: {resetText}
              </Text>
            </Stack>
            <Badge variant="light">{data?.game.title ?? `Game ${gameId}`}</Badge>
          </Group>

          <SimpleGrid cols={{ base: 1, md: 2 }}>
            <NumberInput
              label={t('admin.submission_control.cooldown_factor', { defaultValue: '冷却错答阈值 / Track' })}
              description={t('admin.submission_control.factor_zero_unlimited', { defaultValue: '继承默认时按启用 Track 数相乘；0 = 不限制' })}
              min={0}
              value={cooldownFactor}
              onChange={(v) => setCooldownFactor(getInputNumber(v))}
            />
            <NumberInput
              label={t('admin.submission_control.challenge_daily_factor', { defaultValue: '每队本题每日提交 / Track' })}
              description={t('admin.submission_control.factor_zero_unlimited', { defaultValue: '继承默认时按启用 Track 数相乘；0 = 不限制' })}
              min={0}
              value={challengeDailyFactor}
              onChange={(v) => setChallengeDailyFactor(getInputNumber(v))}
            />
            <NumberInput
              label={t('admin.submission_control.challenge_total_factor', { defaultValue: '每队本题累计提交 / Track' })}
              description={t('admin.submission_control.factor_zero_unlimited', { defaultValue: '继承默认时按启用 Track 数相乘；0 = 不限制' })}
              min={0}
              value={challengeTotalFactor}
              onChange={(v) => setChallengeTotalFactor(getInputNumber(v))}
            />
            <NumberInput
              label={t('admin.submission_control.game_daily_team_limit', { defaultValue: '每队每日比赛总提交上限' })}
              description={t('admin.submission_control.zero_unlimited', { defaultValue: '0 = 不限制，保存后下一次提交热生效' })}
              min={0}
              value={dailyTeamLimit}
              onChange={(v) => setDailyTeamLimit(getInputNumber(v))}
            />
            <NumberInput
              label={t('admin.submission_control.cooldown_minutes', { defaultValue: '分钟级冷却时长' })}
              description={t('admin.submission_control.cooldown_minutes_desc', {
                defaultValue: '错答达到阈值后，该队该题进入冷却；0 = 关闭冷却',
              })}
              min={0}
              value={cooldownMinutes}
              onChange={(v) => setCooldownMinutes(getInputNumber(v))}
            />
          </SimpleGrid>
          <Group justify="flex-end" mt="md">
            <Button
              variant="light"
              leftSection={<Icon path={mdiCogSyncOutline} size={1} />}
              onClick={applyDefaults}
              loading={saving}
            >
              {t('admin.submission_control.apply_defaults_to_all', { defaultValue: '应用到所有题目' })}
            </Button>
          </Group>
        </Card>

        <Card withBorder p="lg">
          <Group justify="space-between" mb="md">
            <Title order={4}>{t('admin.submission_control.challenge_matrix', { defaultValue: '题目控制矩阵' })}</Title>
            <Text size="sm" c="dimmed">
              {t('admin.submission_control.default_factor_hint', {
                defaultValue: '继承默认时按启用 Track 数 × 顶部系数动态计算',
              })}
            </Text>
          </Group>

          <ScrollArea>
            <Table striped highlightOnHover miw={1040}>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('admin.submission_control.challenge', { defaultValue: '题目' })}</Table.Th>
                  <Table.Th>{t('admin.submission_control.tracks', { defaultValue: 'Track' })}</Table.Th>
                  <Table.Th>{t('admin.submission_control.cooldown_threshold', { defaultValue: '冷却错答阈值' })}</Table.Th>
                  <Table.Th>{t('admin.submission_control.daily_team', { defaultValue: '每队本题每日' })}</Table.Th>
                  <Table.Th>{t('admin.submission_control.total_team', { defaultValue: '每队本题累计' })}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {challenges.map((challenge) => (
                  <Table.Tr key={challenge.challengeId}>
                    <Table.Td miw={220}>
                      <Stack gap={2}>
                        <Group gap="xs">
                          <Text fw={700} size="sm">{challenge.title}</Text>
                          {!challenge.isEnabled && <Badge color="gray" variant="light">Disabled</Badge>}
                        </Group>
                        <Text size="xs" c="dimmed">{challenge.category}</Text>
                      </Stack>
                    </Table.Td>
                    <Table.Td>{challenge.trackCount}</Table.Td>
                    <Table.Td>
                      <LimitEditor
                        limit={challenge.cooldownWrongAnswerLimit}
                        trackCount={challenge.trackCount}
                        factor={cooldownFactor}
                        onChange={(limit) => updateChallenge(challenge.challengeId, {
                          cooldownWrongAnswerLimit: limit,
                          cooldownWrongAnswerLimitOverride: limit.customValue,
                          effectiveWrongAnswerLimit: limit.effectiveValue,
                        })}
                      />
                    </Table.Td>
                    <Table.Td>
                      <LimitEditor
                        limit={challenge.dailyTeamSubmission}
                        trackCount={challenge.trackCount}
                        factor={challengeDailyFactor}
                        onChange={(limit) => updateChallenge(challenge.challengeId, {
                          dailyTeamSubmission: limit,
                          dailyTeamSubmissionLimit: limit.effectiveValue,
                        })}
                      />
                    </Table.Td>
                    <Table.Td>
                      <LimitEditor
                        limit={challenge.totalTeamSubmission}
                        trackCount={challenge.trackCount}
                        factor={challengeTotalFactor}
                        onChange={(limit) => updateChallenge(challenge.challengeId, {
                          totalTeamSubmission: limit,
                          totalTeamSubmissionLimit: limit.effectiveValue,
                        })}
                      />
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
        </Card>

        <Card withBorder p="lg">
          <Group justify="space-between" mb="md">
            <Title order={4}>{t('admin.submission_control.team_stats', { defaultValue: '队伍提交状态' })}</Title>
            <Text size="sm" c="dimmed">
              {t('admin.submission_control.poll_hint', { defaultValue: '页面每 15 秒自动刷新，也可手动刷新' })}
            </Text>
          </Group>
          <ScrollArea>
            <Table striped highlightOnHover miw={1040}>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('admin.submission_control.team', { defaultValue: '队伍' })}</Table.Th>
                  <Table.Th>{t('admin.submission_control.status', { defaultValue: '状态' })}</Table.Th>
                  <Table.Th>{t('admin.submission_control.today_total', { defaultValue: '今日/总提交' })}</Table.Th>
                  <Table.Th>{t('admin.submission_control.result_breakdown', { defaultValue: '正确/错误' })}</Table.Th>
                  <Table.Th>{t('admin.submission_control.game_daily_remaining', { defaultValue: '比赛每日剩余' })}</Table.Th>
                  <Table.Th>{t('admin.submission_control.blocked', { defaultValue: '受限题目' })}</Table.Th>
                  <Table.Th>{t('admin.submission_control.cooling', { defaultValue: '冷却中' })}</Table.Th>
                  <Table.Th>{t('admin.submission_control.last_submission', { defaultValue: '最后提交' })}</Table.Th>
                  <Table.Th>{t('admin.submission_control.actions', { defaultValue: '操作' })}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {data?.teams.length === 0 && (
                  <Table.Tr>
                    <Table.Td colSpan={9} align="center" style={{ opacity: 0.6 }}>
                      {t('admin.submission_control.empty_teams', { defaultValue: '暂无队伍' })}
                    </Table.Td>
                  </Table.Tr>
                )}
                {data?.teams.map((team) => (
                  <Table.Tr key={team.participationId}>
                    <Table.Td><Text fw={700}>{team.teamName}</Text></Table.Td>
                    <Table.Td>
                      <Badge color={statusColor(team.participationStatus)} variant="light">
                        {team.participationStatus}
                      </Badge>
                    </Table.Td>
                    <Table.Td>{team.todaySubmissions} / {team.totalSubmissions}</Table.Td>
                    <Table.Td>
                      <Group gap="xs">
                        <Badge color="green" variant="light">{team.acceptedSubmissions}</Badge>
                        <Badge color="red" variant="light">{team.wrongSubmissions}</Badge>
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Badge color={team.gameDailyExceeded ? 'red' : 'blue'} variant="light">
                        {team.gameDailyLimit > 0 ? team.gameDailyRemaining : '-'}
                      </Badge>
                    </Table.Td>
                    <Table.Td>{team.blockedChallenges}</Table.Td>
                    <Table.Td>{team.coolingChallenges}</Table.Td>
                    <Table.Td>{toDateTime(team.lastSubmissionAt)}</Table.Td>
                    <Table.Td>
                      <Button size="xs" variant="light" loading={detailLoading} onClick={() => openDetail(team.teamId)}>
                        {t('admin.submission_control.detail', { defaultValue: '详情' })}
                      </Button>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
        </Card>
      </Stack>

      <Modal
        opened={detail !== null}
        onClose={() => setDetail(null)}
        title={detail ? `${detail.teamName} - ${t('admin.submission_control.team_detail', { defaultValue: '提交控制详情' })}` : ''}
        size="90vw"
      >
        {detail && (
          <ScrollArea h="70vh">
            <Stack gap="sm">
              {detail.gameDailyExceeded && (
                <Alert color="red" variant="light">
                  {t('admin.submission_control.game_daily_blocked', { defaultValue: '该队伍已达到比赛每日总提交上限' })}
                </Alert>
              )}
              <Table striped highlightOnHover miw={1180}>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>{t('admin.submission_control.challenge', { defaultValue: '题目' })}</Table.Th>
                    <Table.Th>{t('admin.submission_control.team_used', { defaultValue: '队伍用量' })}</Table.Th>
                    <Table.Th>{t('admin.submission_control.cooldown', { defaultValue: '冷却' })}</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {detail.challenges.map((challenge) => (
                    <Table.Tr key={challenge.challengeId}>
                      <Table.Td>
                        <Stack gap={2}>
                          <Text fw={700}>{challenge.title}</Text>
                          <Text size="xs" c="dimmed">{challenge.category}</Text>
                        </Stack>
                      </Table.Td>
                      <Table.Td>
                        <Group gap="xs">
                          <Badge color={challenge.dailyTeamExceeded ? 'red' : 'blue'} variant="light">
                            D {challenge.todayTeamUsed}/{challenge.dailyTeamLimit || '-'}
                          </Badge>
                          <Badge color={challenge.totalTeamExceeded ? 'red' : 'blue'} variant="light">
                            T {challenge.totalTeamUsed}/{challenge.totalTeamLimit || '-'}
                          </Badge>
                        </Group>
                      </Table.Td>
                      <Table.Td>
                        <Stack gap={2}>
                          <Group gap="xs">
                            <Badge color={challenge.isCoolingDown ? 'orange' : 'gray'} variant="light">
                              {challenge.wrongCount}/{challenge.effectiveWrongAnswerLimit}
                            </Badge>
                            <Text size="xs" c="dimmed">
                              {t('admin.submission_control.effective_threshold', { defaultValue: '实际阈值' })}
                            </Text>
                          </Group>
                          {challenge.isCoolingDown && (
                            <Text size="xs" c="orange">
                              {t('admin.submission_control.cooldown_until', { defaultValue: '冷却至' })}: {toDateTime(challenge.cooldownUntil)}
                            </Text>
                          )}
                        </Stack>
                      </Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </Stack>
          </ScrollArea>
        )}
      </Modal>
    </WithGameEditTab>
  )
}

export default SubmissionControlPage
