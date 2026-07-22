import {
  Badge, Button, Card, Code, Group, Modal, Pagination, Progress, ScrollArea, Select, SimpleGrid,
  Stack, Table, Tabs, Text, TextInput, Title,
} from '@mantine/core'
import { FC, useCallback, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import { AnswerResult, ParticipationStatus } from '@Api'
import {
  getTeamProgressDetail,
  listTeamProgress,
  TeamProgressDetailDto,
  TeamProgressSummaryDto,
} from '@Utils/AdminTeamProgressApi'

const pageSize = 20

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

const answerColor = (status: AnswerResult) => {
  switch (status) {
    case AnswerResult.Accepted:
      return 'green'
    case AnswerResult.WrongAnswer:
      return 'red'
    case AnswerResult.CheatDetected:
      return 'orange'
    default:
      return 'gray'
  }
}

const TeamProgress: FC = () => {
  const { t } = useTranslation()
  const { id } = useParams()
  const gameId = Number(id)

  const [items, setItems] = useState<TeamProgressSummaryDto[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState<ParticipationStatus | ''>('')
  const [loading, setLoading] = useState(false)
  const [detailLoading, setDetailLoading] = useState(false)
  const [detail, setDetail] = useState<TeamProgressDetailDto | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const res = await listTeamProgress(gameId, { page, pageSize, q: query, status })
      setItems(res.data)
      setTotal(res.total)
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setLoading(false)
    }
  }, [gameId, page, query, status, t])

  useEffect(() => { load() }, [load])

  const openDetail = async (teamId: number) => {
    setDetailLoading(true)
    try {
      setDetail(await getTeamProgressDetail(gameId, teamId))
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDetailLoading(false)
    }
  }

  const statusOptions = useMemo(() => [
    { value: '', label: t('admin.team_progress.filters.all_status') },
    { value: ParticipationStatus.Accepted, label: t('admin.team_progress.status.accepted') },
    { value: ParticipationStatus.Pending, label: t('admin.team_progress.status.pending') },
    { value: ParticipationStatus.Rejected, label: t('admin.team_progress.status.rejected') },
    { value: ParticipationStatus.Suspended, label: t('admin.team_progress.status.suspended') },
  ], [t])

  const rows = items.map((item) => {
    const levelPct = item.totalLevels > 0 ? (item.solvedLevels / item.totalLevels) * 100 : 0
    return (
      <Table.Tr key={item.participationId}>
        <Table.Td>
          <Stack gap={2}>
            <Text fw={700}>{item.teamName}</Text>
            <Code>{item.teamToken}</Code>
          </Stack>
        </Table.Td>
        <Table.Td>
          <Badge color={statusColor(item.participationStatus)} variant="light">
            {t(`admin.team_progress.status.${item.participationStatus.toLowerCase()}`)}
          </Badge>
        </Table.Td>
        <Table.Td>{item.members.join(', ') || '-'}</Table.Td>
        <Table.Td>{item.totalSubmissions}</Table.Td>
        <Table.Td>
          <Group gap={4}>
            <Badge color="green" variant="light">{item.acceptedSubmissions}</Badge>
            <Badge color="red" variant="light">{item.wrongSubmissions}</Badge>
            <Badge color="orange" variant="light">{item.penaltySubmissions}</Badge>
          </Group>
        </Table.Td>
        <Table.Td miw={160}>
          <Text size="sm">{item.solvedLevels} / {item.totalLevels}</Text>
          <Progress value={levelPct} size="sm" />
        </Table.Td>
        <Table.Td>{item.completedTracks} / {item.totalTracks}</Table.Td>
        <Table.Td>{item.zeroDayApprovedCount}</Table.Td>
        <Table.Td>{item.zeroDayScore}</Table.Td>
        <Table.Td>{toDateTime(item.lastSubmissionAt)}</Table.Td>
        <Table.Td>
          <Button size="xs" variant="light" loading={detailLoading} onClick={() => openDetail(item.teamId)}>
            {t('admin.team_progress.actions.detail')}
          </Button>
        </Table.Td>
      </Table.Tr>
    )
  })

  return (
    <WithGameEditTab isLoading={loading}>
      <Card withBorder p="lg">
        <Group mb="md" align="end">
          <TextInput
            label={t('admin.team_progress.filters.search')}
            placeholder={t('admin.team_progress.filters.search_placeholder')}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                setPage(1)
                load()
              }
            }}
            style={{ flex: 1 }}
          />
          <Select
            label={t('admin.team_progress.filters.status')}
            data={statusOptions}
            value={status}
            onChange={(v) => {
              setStatus((v ?? '') as ParticipationStatus | '')
              setPage(1)
            }}
            w={180}
          />
          <Button onClick={() => { setPage(1); load() }}>
            {t('admin.team_progress.actions.refresh')}
          </Button>
        </Group>

        <ScrollArea>
          <Table striped highlightOnHover miw={1180}>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>{t('admin.team_progress.columns.team')}</Table.Th>
                <Table.Th>{t('admin.team_progress.columns.status')}</Table.Th>
                <Table.Th>{t('admin.team_progress.columns.members')}</Table.Th>
                <Table.Th>{t('admin.team_progress.columns.submissions')}</Table.Th>
                <Table.Th>{t('admin.team_progress.columns.result_breakdown')}</Table.Th>
                <Table.Th>{t('admin.team_progress.columns.level_progress')}</Table.Th>
                <Table.Th>{t('admin.team_progress.columns.track_progress')}</Table.Th>
                <Table.Th>{t('admin.team_progress.columns.zeroday_count')}</Table.Th>
                <Table.Th>{t('admin.team_progress.columns.zeroday_score')}</Table.Th>
                <Table.Th>{t('admin.team_progress.columns.last_submission')}</Table.Th>
                <Table.Th>{t('admin.team_progress.columns.actions')}</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {items.length === 0 && !loading && (
                <Table.Tr>
                  <Table.Td colSpan={11} align="center" style={{ opacity: 0.6 }}>
                    {t('admin.team_progress.empty')}
                  </Table.Td>
                </Table.Tr>
              )}
              {rows}
            </Table.Tbody>
          </Table>
        </ScrollArea>

        <Group justify="center" mt="md">
          <Pagination total={Math.ceil(total / pageSize)} value={page} onChange={setPage} />
        </Group>
      </Card>

      <Modal
        opened={detail !== null}
        onClose={() => setDetail(null)}
        title={detail ? `${detail.teamName} - ${t('admin.team_progress.detail.title')}` : ''}
        size="90vw"
      >
        {detail && (
          <Tabs defaultValue="progress">
            <Tabs.List>
              <Tabs.Tab value="progress">{t('admin.team_progress.detail.progress')}</Tabs.Tab>
              <Tabs.Tab value="submissions">{t('admin.team_progress.detail.submissions')}</Tabs.Tab>
              <Tabs.Tab value="zeroday">{t('admin.team_progress.detail.zeroday')}</Tabs.Tab>
            </Tabs.List>

            <Tabs.Panel value="progress" pt="md">
              <ScrollArea h="65vh">
                <Stack gap="md">
                  {detail.challenges.map((challenge) => (
                    <Card key={challenge.challengeId} withBorder p="md">
                      <Group justify="space-between" mb="sm">
                        <Title order={5}>{challenge.challengeTitle}</Title>
                        <Badge>{challenge.category}</Badge>
                      </Group>
                      <Stack gap="sm">
                        {challenge.tracks.map((track) => (
                          <Card key={track.trackId} withBorder p="sm">
                            <Group justify="space-between" align="flex-start">
                              <Stack gap={2}>
                                <Text fw={700}>{track.title || track.filePath || track.trackId}</Text>
                                <Text size="xs" c="dimmed">
                                  {[track.repository, track.module, track.filePath].filter(Boolean).join(' / ') || track.trackId}
                                </Text>
                              </Stack>
                              {!track.isEnabled && <Badge color="gray">{t('admin.team_progress.detail.disabled')}</Badge>}
                            </Group>
                            <SimpleGrid cols={{ base: 1, md: 3 }} mt="sm">
                              {track.levels.map((level) => (
                                <Card key={`${track.trackId}-${level.level}`} withBorder p="sm">
                                  <Group justify="space-between">
                                    <Text fw={700}>L{level.level} {level.title ?? ''}</Text>
                                    <Badge color={level.status === 'Solved' ? 'green' : 'gray'}>
                                      {level.status === 'Solved'
                                        ? t('admin.team_progress.detail.solved')
                                        : t('admin.team_progress.detail.unsolved')}
                                    </Badge>
                                  </Group>
                                  <Text size="sm">{t('admin.team_progress.detail.score')}: {level.score}</Text>
                                  <Text size="sm">
                                    {t('admin.team_progress.detail.attempts')}: {level.attempts}
                                    {level.wrongAttempts > 0 ? ` / ${t('admin.team_progress.detail.wrong')}: ${level.wrongAttempts}` : ''}
                                  </Text>
                                  {level.solvedAt && <Text size="sm">{t('admin.team_progress.detail.solved_at')}: {toDateTime(level.solvedAt)}</Text>}
                                  {level.acceptedAnswer && <Code block>{level.acceptedAnswer}</Code>}
                                </Card>
                              ))}
                            </SimpleGrid>
                          </Card>
                        ))}
                      </Stack>
                    </Card>
                  ))}
                </Stack>
              </ScrollArea>
            </Tabs.Panel>

            <Tabs.Panel value="submissions" pt="md">
              <ScrollArea h="65vh">
                <Table striped highlightOnHover miw={980}>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th>{t('admin.team_progress.submissions.time')}</Table.Th>
                      <Table.Th>{t('admin.team_progress.submissions.user')}</Table.Th>
                      <Table.Th>{t('admin.team_progress.submissions.challenge')}</Table.Th>
                      <Table.Th>Track</Table.Th>
                      <Table.Th>Level</Table.Th>
                      <Table.Th>{t('admin.team_progress.submissions.answer')}</Table.Th>
                      <Table.Th>{t('admin.team_progress.submissions.status')}</Table.Th>
                      <Table.Th>{t('admin.team_progress.submissions.score')}</Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {detail.submissions.map((submission) => (
                      <Table.Tr key={submission.submissionId}>
                        <Table.Td>{toDateTime(submission.submittedAt)}</Table.Td>
                        <Table.Td>{submission.userName}</Table.Td>
                        <Table.Td>{submission.challengeTitle}</Table.Td>
                        <Table.Td><Code>{submission.trackId ?? '-'}</Code></Table.Td>
                        <Table.Td>{submission.level ?? '-'}</Table.Td>
                        <Table.Td maw={340}><Code block>{submission.answer}</Code></Table.Td>
                        <Table.Td>
                          <Badge color={answerColor(submission.status)}>{submission.status}</Badge>
                        </Table.Td>
                        <Table.Td>{submission.scoreChange}</Table.Td>
                      </Table.Tr>
                    ))}
                  </Table.Tbody>
                </Table>
              </ScrollArea>
            </Tabs.Panel>

            <Tabs.Panel value="zeroday" pt="md">
              <Table striped highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>{t('admin.score_ledger.time')}</Table.Th>
                    <Table.Th>{t('admin.score_ledger.delta')}</Table.Th>
                    <Table.Th>{t('admin.zeroday_bonus.cve_severity')}</Table.Th>
                    <Table.Th>{t('admin.score_ledger.reason')}</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {detail.zeroDayBonuses.length === 0 && (
                    <Table.Tr>
                      <Table.Td colSpan={4} align="center" style={{ opacity: 0.6 }}>
                        {t('admin.zeroday_bonus.empty')}
                      </Table.Td>
                    </Table.Tr>
                  )}
                  {detail.zeroDayBonuses.map((bonus) => (
                    <Table.Tr key={bonus.id}>
                      <Table.Td>{toDateTime(bonus.createdAt)}</Table.Td>
                      <Table.Td>{bonus.delta}</Table.Td>
                      <Table.Td>{[bonus.severity, bonus.relatedInfo].filter(Boolean).join(' / ') || '-'}</Table.Td>
                      <Table.Td>{bonus.reason}</Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </Tabs.Panel>
          </Tabs>
        )}
      </Modal>
    </WithGameEditTab>
  )
}

export default TeamProgress
